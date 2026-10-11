using System.Collections.Generic;
using DONT_TOUCH.Enums;
using DONT_TOUCH.Scripts.Editors;

namespace DONT_TOUCH.Scripts.BlockComponents.Locker
{
    [System.Serializable]
    public class LockerChamber
    {
        [SearchableEnum]
        public List<ItemType> AcceptableItems;
        public bool IsOpen;
        public DoorPermissionFlags RequiredPermissions;
    }
}