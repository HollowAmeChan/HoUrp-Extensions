namespace HoUrp.Extensions.Core
{
    public enum HoUrpPassStage
    {
        FrameCameraInit,
        ObjectSemanticBinding,
        ShadowLightingPrepass,
        GeometrySemanticAov,
        OpaqueShading,
        MaterialShadingSemanticAov,
        ScreenSss,
        TransparentOit,
        CharacterComposite,
        SemanticPost,
        ImagePost,
        DebugComposite,
        FinalOutput
    }
}
