using System.Collections.Generic;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;

namespace MockFramework
{
	public class ThingCache
	{
		private readonly IDictionary<string, Thing> dictionary
			= new Dictionary<string, Thing>();
		private readonly IThingService thingService;

		public ThingCache(IThingService thingService)
		{
			this.thingService = thingService;
		}

		public Thing Get(string thingId)
		{
			Thing thing;
			if (dictionary.TryGetValue(thingId, out thing))
				return thing;
			if (thingService.TryRead(thingId, out thing))
			{
				dictionary[thingId] = thing;
				return thing;
			}
			return null;
		}
	}

	[TestFixture]
	public class ThingCache_Should
	{
		private IThingService thingService;
		private ThingCache thingCache;

		private const string thingId1 = "TheDress";
		private Thing thing1 = new Thing(thingId1);

		private const string thingId2 = "CoolBoots";
		private Thing thing2 = new Thing(thingId2);

		[SetUp]
		public void SetUp()
		{
			thingService = A.Fake<IThingService>();
			thingCache = new ThingCache(thingService);
		}

		[Test]
		public void Cache_Should_CallService_When_NoKeyFoundInside()
		{
			thingCache.Get(thingId1);
			A.CallTo(()=>thingService.TryRead(thingId1, out thing1)).MustHaveHappened();
		}

		[Test]
		public void Cache_ShouldNot_CallService_When_KeyFoundInside()
		{
			A.CallTo(() => thingService.TryRead(thingId1, out thing1))
				.Returns(true);

			thingCache.Get(thingId1);
			thingCache.Get(thingId1);

			A.CallTo(() => thingService.TryRead(thingId1, out thing1))
				.MustHaveHappened(Repeated.Exactly.Once);
		}

		[Test]
		public void DoNotCall_TryRead_If_NotRequiered()
		{
			A.CallTo(() => thingService.TryRead(thingId1, out thing1))
				.Returns(true);
			A.CallTo(() => thingService.TryRead(thingId2, out thing2))
				.Returns(true);

			thingCache.Get(thingId1);
			thingCache.Get(thingId1);

			A.CallTo(() => thingService.TryRead(thingId2, out thing2))
				.MustHaveHappened(Repeated.Never);
		}

		[Test]
		public void Cache_ReturnsNull_When_KeyNotFoundInService()
		{
			A.CallTo(() => thingService.TryRead(thingId1, out thing1))
				.Returns(false);
			thingCache.Get(thingId1).Should().BeNull();
		}

		[Test]
		public void DoNotCache_when_ItemDoesNotExists()
		{
			A.CallTo(() => thingService.TryRead(thingId1, out thing1))
				.Returns(false);
			thingCache.Get(thingId1);
			thingCache.Get(thingId1);
			A.CallTo(() => thingService.TryRead(thingId1, out thing1))
				.MustHaveHappened(Repeated.Exactly.Twice);
		}
		[Test]
		public void Cache_ReturnsCorrectThing()
		{
			const string id = "BackPack";
			var newThing = new Thing(id);
			A.CallTo(() => thingService.TryRead(id, out newThing))
				.Returns(true);
			thingCache.Get(id).Should().Be(newThing);
		}


		//TODO: написать простейший тест, а затем все остальные
		//Live Template tt работает!
	}
}