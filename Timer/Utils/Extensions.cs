using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Timer.model;

namespace Timer.Utils;

public static class Extensions {

    public static IList<T> Except<T>(this IList<T> list, T item) => list.Except(new List<T> { item }).ToList();

    public static bool IsParallel(this Step step) => step.IsParallelStart() || step.IsParallelEnd();
        
    public static bool IsParallelStart(this Step step) => step is Step.WAIT_FOR_REVIEW__START or Step.LOADING__START;

    public static bool IsParallelEnd(this Step step) => step is Step.WAIT_FOR_REVIEW__END or Step.LOADING__END;

    public static bool IsSequential(this Step step) => !step.IsParallel() && step != Step.PAUSE;

    public static Step GetEndStepOfParallelStart(this Step startStep) => startStep switch {
        Step.WAIT_FOR_REVIEW__START => Step.WAIT_FOR_REVIEW__END,
        Step.LOADING__START => Step.LOADING__END,
        _ => throw new InvalidEnumArgumentException($"The start Step is not parallel: ${startStep}")
    };

    public static Step GetStartStepOfParallelEnd(this Step endStep) => endStep switch {
        Step.WAIT_FOR_REVIEW__END => Step.WAIT_FOR_REVIEW__START,
        Step.LOADING__END => Step.LOADING__START,
        _ => throw new InvalidEnumArgumentException($"The end Step is not parallel: ${endStep}")
    };

    public static bool IsNullOrEmpty(this string? s) => s is null or "";
}