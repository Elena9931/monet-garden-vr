using UnityEngine;

namespace MonetGarden
{
    [CreateAssetMenu(menuName = "Monet Garden/Artwork")]
    public class GardenArtwork : ScriptableObject
    {
        public string locationName = "Giverny, France";
        public Vector2 latitudeLongitude = new Vector2(49.075f, 1.534f);
        public string title = "Original artwork pending selection";
        public string year;
        [TextArea] public string sourceAndRights;
        public Texture2D originalPainting;
        [Tooltip("Future scene entry. Empty means use this garden.")]
        public string scenePath;
        public bool HasDocumentedOriginal => originalPainting && !string.IsNullOrWhiteSpace(year) &&
            !string.IsNullOrWhiteSpace(sourceAndRights) && !string.IsNullOrWhiteSpace(title);
    }
}
