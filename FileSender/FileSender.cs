using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using FakeItEasy;
using FileSender.Dependencies;
using FluentAssertions;
using NUnit.Framework;

namespace FileSender
{
	public class FileSender
	{
		private readonly ICryptographer cryptographer;
		private readonly ISender sender;
		private readonly IRecognizer recognizer;

		public FileSender(ICryptographer cryptographer,
			ISender sender,
			IRecognizer recognizer)
		{
			this.cryptographer = cryptographer;
			this.sender = sender;
			this.recognizer = recognizer;
		}

		public Result SendFiles(File[] files, X509Certificate certificate)
		{
			return new Result
			{
				SkippedFiles = files
					.Where(file => !TrySendFile(file, certificate))
					.ToArray()
			};
		}

		private bool TrySendFile(File file, X509Certificate certificate)
		{
			Document document;
			if (!recognizer.TryRecognize(file, out document))
				return false;
			if (!CheckFormat(document) || !CheckActual(document))
				return false;
			var signedContent = cryptographer.Sign(document.Content, certificate);
			return sender.TrySend(signedContent);
		}

		private bool CheckFormat(Document document)
		{
			return document.Format == "4.0" ||
				   document.Format == "3.1";
		}

		private bool CheckActual(Document document)
		{
			return document.Created.AddMonths(1) > DateTime.Now;
		}

		public class Result
		{
			public File[] SkippedFiles { get; set; }
		}
	}

	[TestFixture]
	public class FileSender_Should
	{
		private FileSender fileSender;
		private ICryptographer cryptographer;
		private ISender sender;
		private IRecognizer recognizer;

		private readonly X509Certificate certificate = new X509Certificate();
		private File file;
		private byte[] signedContent;

		private Document document;

		[SetUp]
		public void SetUp()
		{
			// Постарайтесь вынести в SetUp всё неспецифическое конфигурирование так,
			// чтобы в конкретных тестах осталась только специфика теста,
			// без конфигурирования "обычного" сценария работы

			file = new File("someFile", new byte[] { 1, 2, 3 });
			signedContent = new byte[] { 1, 7 };

			cryptographer = A.Fake<ICryptographer>();
			sender = A.Fake<ISender>();
			recognizer = A.Fake<IRecognizer>();
			fileSender = new FileSender(cryptographer, sender, recognizer);

			document = new Document(file.Name, file.Content, DateTime.Now, "4.0");

			A.CallTo(() => sender.TrySend(signedContent))
				.Returns(true);
			Document _;
			A.CallTo(() => recognizer.TryRecognize(file, out _))
				.Returns(true).AssignsOutAndRefParametersLazily(__ => new[] {document});

			A.CallTo(() => cryptographer.Sign(document.Content, certificate))
				.Returns(signedContent);

		}

		[TestCase("4.0")]
		[TestCase("3.1")]
		public void Send_WhenGoodFormat(string format)
		{
			document = new Document(document.Name, document.Content,
				document.Created, format);

			fileSender.SendFiles(new[] { file }, certificate)
				.SkippedFiles.Should().BeEmpty();
		}

		[TestCase("3.3")]
		[TestCase("4.3")]
		public void Skip_WhenBadFormat(string format)
		{
			document = new Document(document.Name, document.Content,
				document.Created, format);

			fileSender.SendFiles(new[] { file }, certificate)
				.SkippedFiles.Should().BeEquivalentTo(file);

			A.CallTo(() => sender.TrySend(signedContent))
				.MustHaveHappened(Repeated.Never);
		}

		[Test]
		public void Skip_WhenOlderThanAMonth()
		{
			document = new Document(document.Name, document.Content, 
				DateTime.Now.AddMonths(-1).AddDays(-1), document.Format);

			fileSender.SendFiles(new[] { file }, certificate)
				.SkippedFiles.Should().BeEquivalentTo(file);
		}

		[Test]
		public void Send_WhenYoungerThanAMonth()
		{
			document = new Document(document.Name, document.Content,
				DateTime.Now.AddMonths(-1).AddDays(1), document.Format);
			fileSender.SendFiles(new[] { file }, certificate)
				.SkippedFiles.Should().BeEmpty();
		}

		[Test]
		public void Skip_WhenSendFails()
		{
			A.CallTo(() => sender.TrySend(signedContent))
				.Returns(false);
			fileSender.SendFiles(new[] { file }, certificate)
				.SkippedFiles.Should().BeEquivalentTo(file);
		}

		[Test]
		public void Skip_WhenNotRecognized()
		{
			A.CallTo(() => recognizer.TryRecognize(file, out document))
				.Returns(false);
			fileSender.SendFiles(new[] { file }, certificate)
				.SkippedFiles.Should().BeEquivalentTo(file);
		}

		[Test]
		public void IndependentlySend_WhenSeveralFiles()
		{
			var file1 = new File("someFile1", new byte[] { 1, 2, 4 });
			var file2 = new File("someFile2", new byte[] { 1, 2, 5 });
			var document1 = new Document(file1.Name, file1.Content, DateTime.Now, "5.0");
			var document2 = new Document(file2.Name, file2.Content, DateTime.Now, "4.0");
			A.CallTo(() => recognizer.TryRecognize(file, out document))
				.Returns(false);
			A.CallTo(() => recognizer.TryRecognize(file1, out document1))
				.Returns(true);
			A.CallTo(() => recognizer.TryRecognize(file2, out document2))
				.Returns(true);
			A.CallTo(() => cryptographer.Sign(document2.Content, certificate))
				.Returns(signedContent);

			fileSender.SendFiles(new[] { file, file1, file2 }, certificate)
				.SkippedFiles.Should().BeEquivalentTo(file, file1);
		}
	}
}
