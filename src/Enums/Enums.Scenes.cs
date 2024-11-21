using Menu;

namespace Pearlcat;

public static partial class Enums
{
    public static class Scenes
    {
        // Select Screen
        public static MenuScene.SceneID Slugcat_Pearlcat { get; } = new(nameof(Slugcat_Pearlcat));
        public static MenuScene.SceneID Slugcat_Pearlcat_Sick { get; } = new(nameof(Slugcat_Pearlcat_Sick));
        public static MenuScene.SceneID Slugcat_Pearlcat_Ascended { get; } = new(nameof(Slugcat_Pearlcat_Ascended));

        // Sleep Screen
        public static MenuScene.SceneID Slugcat_Pearlcat_Sleep { get; } = new(nameof(Slugcat_Pearlcat_Sleep));

        // Statistics Screen
        public static MenuScene.SceneID Slugcat_Pearlcat_Statistics_Ascended { get; } = new(nameof(Slugcat_Pearlcat_Statistics_Ascended));
        public static MenuScene.SceneID Slugcat_Pearlcat_Statistics_Sick { get; } = new(nameof(Slugcat_Pearlcat_Statistics_Sick));

        // Cutscene
        public static SlideShow.SlideShowID Pearlcat_AltOutro { get; } = new(nameof(Pearlcat_AltOutro));

        // Dreams
        public static MenuScene.SceneID Dream_Pearlcat_Pearlpup { get; } = new(nameof(Dream_Pearlcat_Pearlpup));
        public static MenuScene.SceneID Dream_Pearlcat_Tower { get; } = new(nameof(Dream_Pearlcat_Tower));

        public static MenuScene.SceneID Dream_Pearlcat_Pebbles { get; } = new(nameof(Dream_Pearlcat_Pebbles));
        public static MenuScene.SceneID Dream_Pearlcat_Moon { get; } = new(nameof(Dream_Pearlcat_Moon));

        public static MenuScene.SceneID Dream_Pearlcat_Sick { get; } = new(nameof(Dream_Pearlcat_Sick));
    }
}
