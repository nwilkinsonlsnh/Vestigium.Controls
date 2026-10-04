using System.Windows.Input;

namespace Vestigium.Controls.QueryBar;

public partial class VestigiumQueryBar
{
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _chevron is { IsChecked: true })
        {
            if (_savedList?.SelectedItem is not null)
                WriteSaved(_savedList.SelectedItem);
            _chevron.IsChecked = false;
        }

        base.OnPreviewKeyDown(e);
    }
}
