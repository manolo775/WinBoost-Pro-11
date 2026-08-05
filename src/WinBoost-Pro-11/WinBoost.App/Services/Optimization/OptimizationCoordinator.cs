using System.Threading.Tasks;

namespace WinBoost.App.Services.Optimization
{
    public sealed class OptimizationCoordinator
    {
        private readonly OptimizationEngine
            _engine;

        public OptimizationCoordinator()
        {
            _engine =
                new OptimizationEngine();
        }

        public OptimizationEngine Engine =>
            _engine;

        public Task<OptimizationReport>
            OptimizeAsync(
                OptimizationOptions options)
        {
            return _engine.RunOptimizationAsync(
                options);
        }
    }
}