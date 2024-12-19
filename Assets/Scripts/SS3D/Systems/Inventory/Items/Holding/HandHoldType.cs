namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Hand holds are used mostly as a way to indicate how items should be held. A toolbox type of hold is on the side of the human,
    /// while a server's plate should be held above the shoulder.
    /// </summary>
    public enum HandHoldType
    {
        None = 0,
        Toolbox = 1,
        Shoulder = 2,
        DoubleHandGun = 3,
        DoubleHandGunHarm = 4,
        SmallItem = 5,
        ThrowToolBox = 6,
        UnderArm = 7,
        ThrowSmallItem = 8,
    }
}
