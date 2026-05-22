namespace HoUrp.Extensions.Core
{
    public enum HoUrpLifetime
    {
        Static,
        PerRenderer,
        PerMaterial,
        PerFrame,
        PerCamera,
        PerPass,
        Transient,
        Persistent,
        Imported
    }
}
