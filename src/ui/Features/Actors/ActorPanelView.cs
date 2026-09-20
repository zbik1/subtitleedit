using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Nikse.SubtitleEdit.Logic;
using Nikse.SubtitleEdit.Logic.Config;
using Nikse.SubtitleEdit.Logic.ValueConverters;

namespace Nikse.SubtitleEdit.Features.Actors;

public static class ActorPanelView
{
    public static Control Make(ActorPanelViewModel vm)
    {
        var itemsControl = new ItemsControl
        {
            // Air between the rows and the scrollbar, which has its own lane (see scrollViewer).
            Margin = new Thickness(0, 0, 6, 0),
            // MainViewModel replaces the whole Actors collection (e.g. SyncActorPanelActors),
            // so this needs a real binding - a plain ItemsSource = vm.Actors would keep pointing
            // at the original, empty collection.
            [!ItemsControl.ItemsSourceProperty] = new Binding(nameof(vm.Actors)),
            ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 }),
            ItemTemplate = new FuncDataTemplate<ActorDisplayItem>((actor, _) =>
            {
                // A TextBlock, not a plain string Content: Avalonia's Button template treats a
                // bare string as access-key text and would eat a literal "_" in the actor name.
                var nameText = new TextBlock
                {
                    Text = actor!.Name,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                };

                // Null = no shortcut slot for this row (past the 10th); empty string = slot
                // exists, nothing assigned yet.
                var shortcutBadge = new Border
                {
                    IsVisible = actor.ShortcutText != null,
                    Background = UiUtil.GetTextColor(0.08d),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(5, 2),
                    Margin = new Thickness(6, 0, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = actor.ShortcutText,
                        FontSize = UiUtil.ScaledFontSize(10),
                        Opacity = 0.7,
                    },
                };

                var buttonContent = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                };
                buttonContent.Add(nameText, 0, 0);
                buttonContent.Add(shortcutBadge, 0, 1);

                var assignButton = new Button
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Padding = new Thickness(10, 6),
                    BorderThickness = new Thickness(0),
                    Background = Brushes.Transparent,
                    Command = vm.SetActorByNameCommand,
                    CommandParameter = actor,
                    Content = buttonContent,
                };

                assignButton.ContextMenu = new ContextMenu
                {
                    Items =
                    {
                        new MenuItem
                        {
                            Header = Se.Language.General.Rename + "...",
                            Command = vm.RenameActorCommand,
                            CommandParameter = actor,
                        },
                    },
                };

                var buttonMoveUp = new Button
                {
                    Content = "▲",
                    FontSize = UiUtil.ScaledFontSize(9),
                    Padding = new Thickness(4, 0),
                    IsVisible = !actor.IsFirst,
                    Command = vm.MoveActorUpCommand,
                    CommandParameter = actor,
                };
                var buttonMoveDown = new Button
                {
                    Content = "▼",
                    FontSize = UiUtil.ScaledFontSize(9),
                    Padding = new Thickness(4, 0),
                    IsVisible = !actor.IsLast,
                    Command = vm.MoveActorDownCommand,
                    CommandParameter = actor,
                };
                var moveButtons = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 1,
                };
                moveButtons.Children.Add(buttonMoveUp);
                moveButtons.Children.Add(buttonMoveDown);

                var subtleBorderBrush = UiUtil.GetTextColor(0.3d);

                // The move buttons sit next to, not inside, assignButton - Avalonia buttons
                // don't nest reliably.
                var rowContent = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                };
                rowContent.Add(assignButton, 0, 0);
                rowContent.Add(moveButtons, 0, 1);

                var rowBorder = new Border
                {
                    BorderThickness = new Thickness(1),
                    BorderBrush = subtleBorderBrush,
                    CornerRadius = new CornerRadius(UiUtil.CornerRadius),
                    Child = rowContent,
                };

                // DataContext here is the row's own ActorDisplayItem, not the panel's vm.
                var accentBrush = UiUtil.GetAccentBrush();
                var accentColor = (accentBrush as SolidColorBrush)?.Color ?? Colors.DodgerBlue;
                rowBorder.Bind(Border.BorderBrushProperty, new Binding(nameof(ActorDisplayItem.IsHighlighted))
                {
                    Converter = new BooleanToBrushConverter
                    {
                        TrueBrush = accentBrush,
                        FalseBrush = subtleBorderBrush,
                    },
                });
                rowBorder.Bind(Border.BackgroundProperty, new Binding(nameof(ActorDisplayItem.IsHighlighted))
                {
                    Converter = new BooleanToBrushConverter
                    {
                        TrueBrush = new SolidColorBrush(accentColor, 0.18),
                        FalseBrush = Brushes.Transparent,
                    },
                });

                return rowBorder;
            }),
        };

        var scrollViewer = new ScrollViewer
        {
            Content = itemsControl,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            // Give the scrollbar its own layout lane: as an auto-hiding overlay it is drawn
            // on top of the content and covered the reorder arrows.
            AllowAutoHide = false,
        };

        var buttonNewActor = UiUtil.MakeButton(Se.Language.General.NewDotDotDot, vm.NewActorCommand);
        buttonNewActor.HorizontalAlignment = HorizontalAlignment.Stretch;
        var buttonClearActor = UiUtil.MakeButton(Se.Language.General.Clear, vm.ClearCommand);
        buttonClearActor.HorizontalAlignment = HorizontalAlignment.Stretch;
        buttonClearActor.WithBindEnabled(nameof(vm.IsClearEnabled));
        var bottomBar = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            ColumnSpacing = 6,
            Margin = new Thickness(0, 8, 0, 0),
        };
        bottomBar.Add(buttonNewActor, 0, 0);
        bottomBar.Add(buttonClearActor, 0, 1);

        // No Width here: MainView puts the panel in its own Grid column, and the GridSplitter
        // next to it resizes that column.
        var grid = new Grid
        {
            Margin = UiUtil.MakeWindowMargin(),
            RowDefinitions = new RowDefinitions("*,Auto"),
            DataContext = vm,
        };
        grid.Add(scrollViewer, 0, 0);
        grid.Add(bottomBar, 1, 0);

        return grid;
    }
}
