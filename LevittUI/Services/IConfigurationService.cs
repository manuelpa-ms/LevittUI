namespace LevittUI.Services
{
    public interface IConfigurationService
    {
        string ServerAddress { get; }
        string DefaultUsername { get; }
        string DefaultPassword { get; }
        bool IsAutoLoginEnabled { get; }
        void SetAutoLoginEnabled(bool enabled);
    }
}
