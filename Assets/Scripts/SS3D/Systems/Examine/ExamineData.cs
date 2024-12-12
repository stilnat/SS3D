using UnityEngine;
using UnityEngine.Localization;

namespace SS3D.Systems.Examine
{
    [CreateAssetMenu(menuName = "Examine", fileName = "ExamineData")]
    public class ExamineData : ScriptableObject
    {
        /// <summary>
        /// The table holding the localization strings for this object.
        /// </summary>
        [field:SerializeField]
        public LocalizedStringTable LocalizationTable { get; private set; }

        /// <summary>
        /// Key to access the name of the object, when the cursor hovers over the item.
        /// </summary>
        [field:SerializeField]
        public string NameKey { get; private set; }

        /// <summary>
        /// Key to access the description shown below the name, when the cursor hovers over the item while holding Shift.
        /// </summary>
        [field:SerializeField]
        public string DescriptionKey { get; private set; }
    }
}
