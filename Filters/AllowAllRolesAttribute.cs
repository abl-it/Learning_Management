namespace Training.Filters
{
    /// <summary>
    /// Marker attribute: action ini boleh diakses SEMUA user yang sudah login
    /// (session Username terisi), meskipun controller-nya dikunci [RoleAuthorize(...)].
    /// Dibaca oleh RoleAuthorizeAttribute via endpoint metadata.
    /// Pasang hanya pada action read-only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AllowAllRolesAttribute : Attribute
    {
    }
}
