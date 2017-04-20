using System.Windows;

namespace svn_assist_client
{
    public static class TreeViewItemProps
    {
        public static readonly DependencyProperty FileTypeProperity =
            DependencyProperty.RegisterAttached(
                "FileType",
                typeof(string),
                typeof(TreeViewItemProps),
                new UIPropertyMetadata("folder")
            );
        public static string GetFileType(DependencyObject obj)
        {
            return (string)obj.GetValue(FileTypeProperity);
        }
        public static void SetFileType(DependencyObject obj, string value)
        {
            obj.SetValue(FileTypeProperity, value);
        }
    }
}
