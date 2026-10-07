using Godot;
using System;
using System.Threading.Tasks;
using Cybin;

public partial class MenuFlow : Node
{
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        CallDeferred(nameof(Run));
    }

    private async Task Frames()
    {
        for (int i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private async void Run()
    {
        try
        {
            var menu = GD.Load<PackedScene>("res://Scenes/UI/MainMenu.tscn").Instantiate<MainMenu>();
            GetTree().Root.AddChild(menu);
            GetTree().CurrentScene = menu;
            await Frames();
            for (int index = 0; index < 2; index++)
            {
                menu = (MainMenu)GetTree().CurrentScene;
                string card = index == 0 ? "Frontier" : "Amber";
                menu.GetNode<Button>($"Margin/Content/Cards/{card}/Content/Select").EmitSignal(Button.SignalName.Pressed);
                Check(menu.SelectedMap == index, "Map selection button");
                menu.GetNode<Button>("Margin/Content/Footer/Play").EmitSignal(Button.SignalName.Pressed);
                await Frames();
                var battle = (Battlefield)GetTree().CurrentScene;
                string title = index == 0 ? "FRONTERA DE TITANIO" : "CUENCA DE ÁMBAR";
                Check(battle.MapTitle == title && battle.Deposits.Count == (index == 0 ? 5 : 7), "Selected map layout");
                Check(battle.Entities.Count == 10 && battle.Income == 16 && battle.Credits == 700, "Fresh battle state");
                battle.Train(UnitKind.Tank);
                battle.Hud.TogglePause();
                Check(GetTree().Paused, "Pause before restart");
                // Same operation used by the restart button: unpause and reload the selected scene.
                GetTree().Paused = false;
                GetTree().ReloadCurrentScene();
                await Frames();
                battle = (Battlefield)GetTree().CurrentScene;
                Check(battle.MapTitle == title && battle.Credits == 700 && battle.Production.Count == 0, "Restart retains map and resets match");
                battle.Hud.TogglePause();
                battle.Hud.ReturnToMenu();
                await Frames();
                menu = (MainMenu)GetTree().CurrentScene;
                Check(!GetTree().Paused && menu.SelectedMap == index, "Return unpauses and remembers selection");
                if (index == 1)
                {
                    menu.StartMatch();
                    await Frames();
                    battle = (Battlefield)GetTree().CurrentScene;
                    battle.EnemyBase.TakeDamage(99999);
                    Check(battle.Ended && battle.Won, "Win on second map");
                    battle.Hud.ReturnToMenu();
                    await Frames();
                    Check(GetTree().CurrentScene is MainMenu && !GetTree().Paused, "Return after victory");
                }
            }
            GD.Print("MENU FLOW PASS: both selection buttons, launch, layouts, fresh state, restart, paused return, victory return.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError("MENU FLOW FAIL: " + error);
            GetTree().Quit(1);
        }
    }
}
