namespace MeloongCore.Tests;

public class ProgressTest : TestBase {

    [Test]
    public async Task ProgressChanged_主进度实际改变时触发() {
        var progress = new ProgressProvider();
        int changedCount = 0;
        progress.ProgressChanged += _ => changedCount++;

        progress.Set(0.2);
        progress.Set(0.2);
        progress.Add(0.1);
        progress.Add(0);
        progress.Set(0.5, skiped: true);

        await Assert.That(changedCount).IsEqualTo(3);
    }

    [Test]
    public async Task ProgressChanged_子进度改变时向父级传播() {
        var progress = new ProgressProvider();
        int changedCount = 0;
        (double actual, double skiped) changedProgress = default;
        progress.ProgressChanged += value => {
            changedCount++;
            changedProgress = value;
        };

        var sub = progress.SplitBy(0.5).Single();
        sub.Set(0.5);
        sub.Set(0.5);

        await Assert.That(changedCount).IsEqualTo(1);
        await Assert.That(changedProgress.actual).IsEqualTo(0.25);
        await Assert.That(changedProgress.skiped).IsEqualTo(0);
    }

    [Test]
    public async Task ProgressChanged_不会触发Observe() {
        var progress = new ProgressProvider();
        int changedCount = 0;
        progress.ProgressChanged += _ => changedCount++;

        progress.Set(0.5);
        progress.Set(0.75, skiped: true);

        await Assert.That(changedCount).IsEqualTo(2);
        await Assert.That(Math.Abs(progress.GetIncreasing() - 2.0 / 3) < 0.000001).IsTrue();
    }

    [Test]
    public async Task ProgressObserver_监听进度变化并更新UI() {
        var progress = new ProgressProvider();
        int updateCount = 0;
        double updatedProgress = 0;

        using (var observer = new ProgressObserver(progress, value => {
            updateCount++;
            updatedProgress = value;
        }, TimeSpan.FromHours(1).TotalMilliseconds)) {
            progress.Set(0.25);

            await Assert.That(updateCount).IsEqualTo(1);
            await Assert.That(updatedProgress).IsEqualTo(0.25);
        }

        progress.Set(0.5);
        await Task.Delay(50);

        await Assert.That(updateCount).IsEqualTo(1);
    }

}
