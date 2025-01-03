using UnityEngine;

namespace SS3D.Traits
{
    [CreateAssetMenu(fileName = "ID Permission", menuName = "Inventory/Traits/Permission")]
    public class IDPermission : Trait
    {
        public IDPermission()
        {
            Category = TraitCategories.IDPermission;
        }
    }
}
