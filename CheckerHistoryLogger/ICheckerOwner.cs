namespace Vishnu_UserModules
{
    /// <summary>
    /// Interface für Checker, die selbst Checker verwalten.
    /// </summary>
    public interface ICheckerOwner
    {
        /// <summary>
        /// Name des verwalteten Checkers.
        /// </summary>
        string CheckerName { get; }
    }
}
