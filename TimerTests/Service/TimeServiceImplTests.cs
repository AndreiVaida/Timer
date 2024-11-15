using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Timer.model;
using Timer.Model;
using Timer.Repository;
using Timer.Utils;

namespace Timer.service.Tests;

[TestClass]
public class TimeServiceImplTests : ReactiveTest {
        
    [TestMethod]
    [DynamicData(nameof(SequentialStepsTestCases))]
    [DynamicData(nameof(ParallelStepsTestCases))]
    [DynamicData(nameof(SequentialAndParallelStepsTestCases))]
    public void GivenExistingSequentialSteps_WhenCreateActivity_ThenCorrectDurationIsCalculatedForEachStep(List<TimeLog> timeLogs, List<TimeEvent> expectedTimeEvents, DateTime now) {
        var timeRepositoryMock = new Mock<TimeRepository>();
        timeRepositoryMock.Setup(repo => repo.GetTimeLogs(null)).Returns(timeLogs);
        var timeUtilsMock = new Mock<TimeUtils>();
        timeUtilsMock.Setup(util => util.CurrentDateTime()).Returns(now);
        var timeService = new TimeServiceImpl(timeRepositoryMock.Object, timeUtilsMock.Object);
        var observer = CreateTestObserver(timeService.TimeUpdates);

        timeService.CreateActivity("");

        Assert.AreEqual(expectedTimeEvents.Count, observer.Messages.Count);
        for (var i = 0; i < observer.Messages.Count; i++) {
            var expectedTimeEvent = expectedTimeEvents[i];
            var timeEvent = observer.Messages[i].Value.Value;
            Assert.AreEqual(expectedTimeEvent, timeEvent);
        }
    }

    public static IEnumerable<object[]> SequentialStepsTestCases => new[] {
        // Each Step appears 1 time
        new object[] {
            new List<TimeLog> {
                new(Step.MEETING,           new DateTime(2023, 01, 28, 10, 18, 00)),
                new(Step.OTHER,             new DateTime(2023, 01, 28, 10, 28, 00)),
                new(Step.INVESTIGATE,       new DateTime(2023, 01, 28, 11, 18, 00)),
                new(Step.IMPLEMENT,         new DateTime(2023, 01, 28, 11, 18, 01)),
                new(Step.RESOLVE_COMMENTS,  new DateTime(2023, 01, 28, 11, 19, 00)),
                new(Step.DO_REVIEW,         new DateTime(2023, 01, 28, 12, 18, 00)),
                new(Step.PAUSE,             new DateTime(2023, 01, 28, 12, 18, 03))
            },
            new List<TimeEvent> {
                new(Step.MEETING,                   TimeSpan.FromMinutes(10), false),
                new(Step.OTHER,                     TimeSpan.FromMinutes(50), false),
                new(Step.INVESTIGATE,               TimeSpan.FromSeconds(1), false),
                new(Step.IMPLEMENT,                 TimeSpan.FromSeconds(59), false),
                new(Step.WAIT_FOR_REVIEW__START,    TimeSpan.Zero, false),
                new(Step.RESOLVE_COMMENTS,          TimeSpan.FromMinutes(59), false),
                new(Step.DO_REVIEW,                 TimeSpan.FromSeconds(3), false),
                new(Step.LOADING__START,            TimeSpan.Zero, false),
                new(Step.TOTAL,                     TimeSpan.FromHours(2).Add(TimeSpan.FromSeconds(3)), false)
            },
            null
        },

        // Steps appears several time
        new object[] {
            new List<TimeLog> {
                new(Step.INVESTIGATE,       new DateTime(2023, 01, 28, 11, 18, 00)),
                new(Step.INVESTIGATE,       new DateTime(2023, 01, 28, 11, 18, 01)),
                new(Step.IMPLEMENT,         new DateTime(2023, 01, 28, 11, 18, 02)),
                new(Step.PAUSE,             new DateTime(2023, 01, 28, 11, 18, 30)),
                new(Step.INVESTIGATE,       new DateTime(2023, 01, 28, 11, 19, 00)),
                new(Step.IMPLEMENT,         new DateTime(2023, 01, 28, 12, 18, 00)),
                new(Step.PAUSE,             new DateTime(2023, 01, 28, 12, 18, 03))
            },
            new List<TimeEvent> {
                new(Step.MEETING,                   TimeSpan.Zero, false),
                new(Step.OTHER,                     TimeSpan.Zero, false),
                new(Step.INVESTIGATE,               TimeSpan.FromSeconds(2).Add(TimeSpan.FromMinutes(59)), false),
                new(Step.IMPLEMENT,                 TimeSpan.FromSeconds(28).Add(TimeSpan.FromSeconds(3)), false),
                new(Step.WAIT_FOR_REVIEW__START,    TimeSpan.Zero, false),
                new(Step.RESOLVE_COMMENTS,          TimeSpan.Zero, false),
                new(Step.DO_REVIEW,                 TimeSpan.Zero, false),
                new(Step.LOADING__START,            TimeSpan.Zero, false),
                new(Step.TOTAL,                     TimeSpan.FromMinutes(59).Add(TimeSpan.FromSeconds(33)), false)
            },
            null
        }
    };

    public static IEnumerable<object[]> ParallelStepsTestCases => new[] {
        // Each Step appears 1 time
        new object[] {
            new List<TimeLog> {
                new(Step.WAIT_FOR_REVIEW__START,    new DateTime(2023, 01, 28, 11, 18, 00)),
                new(Step.WAIT_FOR_REVIEW__END,      new DateTime(2023, 01, 28, 11, 18, 01)),
                new(Step.LOADING__START,            new DateTime(2023, 01, 28, 11, 19, 00)),
                new(Step.LOADING__END,              new DateTime(2023, 01, 28, 12, 18, 00))
            },
            new List<TimeEvent> {
                new(Step.MEETING,                   TimeSpan.Zero, false),
                new(Step.OTHER,                     TimeSpan.Zero, false),
                new(Step.INVESTIGATE,               TimeSpan.Zero, false),
                new(Step.IMPLEMENT,                 TimeSpan.Zero, false),
                new(Step.WAIT_FOR_REVIEW__START,    TimeSpan.FromSeconds(1), false),
                new(Step.RESOLVE_COMMENTS,          TimeSpan.Zero, false),
                new(Step.DO_REVIEW,                 TimeSpan.Zero, false),
                new(Step.LOADING__START,            TimeSpan.FromMinutes(59), false),
                new(Step.TOTAL,                     TimeSpan.FromSeconds(1).Add(TimeSpan.FromMinutes(59)), false)
            },
            null
        },

        // Steps appears several time
        new object[] {
            new List<TimeLog> {
                new(Step.WAIT_FOR_REVIEW__START,    new DateTime(2023, 01, 28, 11, 18, 00)),
                new(Step.WAIT_FOR_REVIEW__END,      new DateTime(2023, 01, 28, 11, 18, 01)),
                new(Step.WAIT_FOR_REVIEW__START,    new DateTime(2023, 01, 28, 11, 18, 02)),
                new(Step.LOADING__START,            new DateTime(2023, 01, 28, 11, 19, 00)),
                new(Step.WAIT_FOR_REVIEW__END,      new DateTime(2023, 01, 28, 12, 18, 02)),
                new(Step.PAUSE,                     new DateTime(2023, 01, 28, 12, 18, 05))
            },
            new List<TimeEvent> {
                new(Step.MEETING,                   TimeSpan.Zero, false),
                new(Step.OTHER,                     TimeSpan.Zero, false),
                new(Step.INVESTIGATE,               TimeSpan.Zero, false),
                new(Step.IMPLEMENT,                 TimeSpan.Zero, false),
                new(Step.WAIT_FOR_REVIEW__START,    TimeSpan.FromSeconds(1).Add(TimeSpan.FromHours(1)), false),
                new(Step.RESOLVE_COMMENTS,          TimeSpan.Zero, false),
                new(Step.DO_REVIEW,                 TimeSpan.Zero, false),
                new(Step.LOADING__START,            TimeSpan.FromMinutes(59).Add(TimeSpan.FromSeconds(5)), false),
                new(Step.TOTAL,                     TimeSpan.FromSeconds(4).Add(TimeSpan.FromHours(1)), false)
            },
            null
        }
    };

    public static IEnumerable<object[]> SequentialAndParallelStepsTestCases => new[] {
        // Scenario 1, no Pause
        new object[] {
            new List<TimeLog> {
                new(Step.IMPLEMENT,                 new DateTime(2023, 01, 28, 11, 18, 00)),
                new(Step.WAIT_FOR_REVIEW__START,    new DateTime(2023, 01, 28, 11, 18, 01)),
                new(Step.DO_REVIEW,                 new DateTime(2023, 01, 28, 11, 18, 02)),
                new(Step.LOADING__START,            new DateTime(2023, 01, 28, 11, 19, 00)),
                new(Step.LOADING__END,              new DateTime(2023, 01, 28, 12, 18, 00)),
                new(Step.DO_REVIEW,                 new DateTime(2023, 01, 28, 12, 18, 03)),
                new(Step.LOADING__START,            new DateTime(2023, 01, 28, 12, 19, 00)),
                new(Step.INVESTIGATE,               new DateTime(2023, 01, 28, 12, 20, 00)),
                new(Step.LOADING__END,              new DateTime(2023, 01, 28, 12, 22, 04)),
                new(Step.WAIT_FOR_REVIEW__END,      new DateTime(2023, 01, 28, 13, 22, 04)),
                new(Step.RESOLVE_COMMENTS,          new DateTime(2023, 01, 28, 13, 25, 00)),
                new(Step.PAUSE,                     new DateTime(2023, 01, 28, 13, 25, 05)),
            },
            new List<TimeEvent> {
                new(Step.MEETING,                   TimeSpan.Zero, false),
                new(Step.OTHER,                     TimeSpan.Zero, false),
                new(Step.INVESTIGATE,               TimeSpan.FromMinutes(65), false),
                new(Step.IMPLEMENT,                 TimeSpan.FromSeconds(1), false),
                new(Step.WAIT_FOR_REVIEW__START,    TimeSpan.FromMinutes(124).Add(TimeSpan.FromSeconds(3)), false),
                new(Step.RESOLVE_COMMENTS,          TimeSpan.FromSeconds(5), false),
                new(Step.DO_REVIEW,                 TimeSpan.FromSeconds(58).Add(TimeSpan.FromSeconds(57)), false),
                new(Step.LOADING__START,            TimeSpan.FromMinutes(59).Add(TimeSpan.FromSeconds(184)), false),
                new(Step.TOTAL,                     TimeSpan.FromHours(2).Add(TimeSpan.FromMinutes(7).Add(TimeSpan.FromSeconds(5))), false)
            },
            null
        },
        
        // Scenario 2, with Pause
        new object[] {
            new List<TimeLog> {
                new(Step.IMPLEMENT,                 new DateTime(2023, 01, 28, 11, 18, 00)),
                new(Step.WAIT_FOR_REVIEW__START,    new DateTime(2023, 01, 28, 11, 18, 01)),
                new(Step.PAUSE,                     new DateTime(2023, 01, 28, 11, 18, 02)),
                new(Step.WAIT_FOR_REVIEW__START,    new DateTime(2023, 01, 28, 11, 18, 05)),
                new(Step.DO_REVIEW,                 new DateTime(2023, 01, 28, 11, 18, 09)),
                new(Step.LOADING__START,            new DateTime(2023, 01, 28, 11, 19, 00)),
                new(Step.WAIT_FOR_REVIEW__END,      new DateTime(2023, 01, 28, 11, 19, 08)),
                new(Step.PAUSE,                     new DateTime(2023, 01, 28, 11, 19, 09)),
                new(Step.DO_REVIEW,                 new DateTime(2023, 01, 28, 12, 18, 03)),
                new(Step.LOADING__START,            new DateTime(2023, 01, 28, 12, 19, 00)),
                new(Step.INVESTIGATE,               new DateTime(2023, 01, 28, 12, 20, 00)),
                new(Step.LOADING__END,              new DateTime(2023, 01, 28, 12, 22, 00)),
                new(Step.RESOLVE_COMMENTS,          new DateTime(2023, 01, 28, 13, 25, 00)),
                new(Step.PAUSE,                     new DateTime(2023, 01, 28, 13, 25, 05)),
            },
            new List<TimeEvent> {
                new(Step.MEETING,                   TimeSpan.Zero, false),
                new(Step.OTHER,                     TimeSpan.Zero, false),
                new(Step.INVESTIGATE,               TimeSpan.FromMinutes(65), false),
                new(Step.IMPLEMENT,                 TimeSpan.FromSeconds(1), false),
                new(Step.WAIT_FOR_REVIEW__START,    TimeSpan.FromSeconds(1)
                                                                    .Add(TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(3))), false),
                new(Step.RESOLVE_COMMENTS,          TimeSpan.FromSeconds(5), false),
                new(Step.DO_REVIEW,                 TimeSpan.FromSeconds(51)
                                                                    .Add(TimeSpan.FromSeconds(57)), false),
                new(Step.LOADING__START,            TimeSpan.FromSeconds(9).Add(TimeSpan.FromMinutes(3)), false),
                new(Step.TOTAL,                     TimeSpan.FromSeconds(2)
                                                                    .Add(TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(4)))
                                                                    .Add(TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(7).Add(TimeSpan.FromSeconds(2)))), false)
            },
            null
        },

        // Scenario 3, no Pause
        new object[] {
            new List<TimeLog> {
                new(Step.IMPLEMENT,                 new DateTime(2023, 01, 28, 11, 18, 00)),
                new(Step.WAIT_FOR_REVIEW__START,    new DateTime(2023, 01, 28, 11, 18, 01)),
                new(Step.DO_REVIEW,                 new DateTime(2023, 01, 28, 11, 18, 09)),
                new(Step.LOADING__START,            new DateTime(2023, 01, 28, 11, 19, 00)),
                new(Step.DO_REVIEW,                 new DateTime(2023, 01, 28, 12, 18, 03)),
                new(Step.INVESTIGATE,               new DateTime(2023, 01, 28, 12, 20, 00)),
            },
            new List<TimeEvent> {
                new(Step.MEETING,                   TimeSpan.Zero, false),
                new(Step.OTHER,                     TimeSpan.Zero, false),
                new(Step.INVESTIGATE,               TimeSpan.FromMinutes(10), true),
                new(Step.IMPLEMENT,                 TimeSpan.FromSeconds(1), false),
                new(Step.WAIT_FOR_REVIEW__START,    TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(11).Add(TimeSpan.FromSeconds(59))), true),
                new(Step.RESOLVE_COMMENTS,          TimeSpan.Zero, false),
                new(Step.DO_REVIEW,                 TimeSpan.FromSeconds(51)
                                                                    .Add(TimeSpan.FromSeconds(117)), false),
                new(Step.LOADING__START,            TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(11)), true),
                new(Step.TOTAL,                     TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(12)), true)
            },
            new DateTime(2023, 1, 28, 12, 30, 0)
        }
    };

    private static ITestableObserver<TimeEvent> CreateTestObserver(IObservable<TimeEvent> observable) {
        var scheduler = new TestScheduler();
        var observer = scheduler.CreateObserver<TimeEvent>();
        scheduler.Schedule(() => observable.Subscribe(observer));
        scheduler.Start();
        return observer;
    }
}