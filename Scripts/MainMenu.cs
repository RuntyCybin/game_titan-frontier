using Godot;
using System.Linq;

namespace Cybin;

public partial class MainMenu : Control
{
    [Export] public PackedScene FrontierScene { get; set; } = null!;
    [Export] public PackedScene AmberScene { get; set; } = null!;
    private static int _lastSelected;
    private Button _frontier = null!, _amber = null!, _play = null!;
    private Label _detail = null!;
    private bool _starting;
    private int _frames;
    public int SelectedMap { get; private set; }

    public override void _Ready()
    {
        GetTree().Paused = false;
        _frontier = GetNode<Button>("Margin/Content/Cards/Frontier/Content/Select");
        _amber = GetNode<Button>("Margin/Content/Cards/Amber/Content/Select");
        _play = GetNode<Button>("Margin/Content/Footer/Play");
        _detail = GetNode<Label>("Margin/Content/Selected");
        _frontier.Pressed += () => SelectMap(0);
        _amber.Pressed += () => SelectMap(1);
        _play.Pressed += StartMatch;
        GetNode<Button>("Margin/Content/Footer/Exit").Pressed += () => GetTree().Quit();
        SelectMap(_lastSelected);
        _play.GrabFocus();
    }

    public void SelectMap(int index)
    {
        SelectedMap = index == 1 ? 1 : 0;
        _lastSelected = SelectedMap;
        _frontier.ButtonPressed = SelectedMap == 0;
        _amber.ButtonPressed = SelectedMap == 1;
        _frontier.Text = SelectedMap == 0 ? "✓  MAPA SELECCIONADO" : "SELECCIONAR MAPA";
        _amber.Text = SelectedMap == 1 ? "✓  MAPA SELECCIONADO" : "SELECCIONAR MAPA";
        string title = SelectedMap == 0 ? "Frontera de Titanio" : "Cuenca de Ámbar";
        _detail.Text = "DESPLIEGUE CONFIRMADO   /   " + title + "   ·   1 jugador contra IA";
        _play.Text = "INICIAR ESCARAMUZA   →";
    }

    public void StartMatch()
    {
        if (_starting) return;
        _starting = true;
        _play.Disabled = true;
        var result = GetTree().ChangeSceneToPacked(SelectedMap == 0 ? FrontierScene : AmberScene);
        if (result != Error.Ok)
        {
            _starting = false; _play.Disabled = false;
            _detail.Text = "No se pudo cargar el mapa. " + result;
        }
    }

    public override void _Process(double delta)
    {
        if (OS.GetCmdlineUserArgs().Contains("--capture-menu") && ++_frames == 30)
        {
            GetViewport().GetTexture().GetImage().SavePng("res://menu-preview.png");
            GetTree().Quit();
        }
    }
}
