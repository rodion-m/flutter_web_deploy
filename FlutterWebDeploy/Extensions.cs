namespace FlutterWebDeploy
{
    public static class Extensions
    {
        public static string? NullIfWhiteSpace(this string? @this)
        {
            return string.IsNullOrWhiteSpace(@this) ? null : @this;
        }
    }
}