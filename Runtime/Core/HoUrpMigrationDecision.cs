namespace HoUrp.Extensions.Core
{
    public enum HoUrpMigrationDecision
    {
        KeepConceptRename,
        KeepConceptTemporaryLegacyBinding,
        Replace,
        Remove,
        Defer,
        ValidationOnly
    }
}
