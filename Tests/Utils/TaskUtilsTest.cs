namespace MeloongCore.Tests;

public class TaskUtilsTest : TestBase {

    [Test]
    public async Task WhenAll_汇总等权子进度并保持结果顺序() {
        var progress = new ProgressProvider();
        var continueSecond = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProgressProvider? firstProgress = null, secondProgress = null;

        var whenAll = TaskUtils.WhenAll<int>([
            child => {
                firstProgress = child;
                return Task.FromResult(1);
            },
            async child => {
                secondProgress = child;
                await continueSecond.Task;
                return 2;
            }
        ], progress);

        await Assert.That(ReferenceEquals(firstProgress, secondProgress)).IsFalse();
        await Assert.That(progress.GetIncreasing()).IsEqualTo(0.5);
        continueSecond.SetResult(null);
        var results = await whenAll;
        await Assert.That(string.Join(",", results)).IsEqualTo("1,2");
        await Assert.That(progress.GetIncreasing()).IsEqualTo(1);
    }

}
