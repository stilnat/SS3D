﻿using SS3D.Data.Enums;
using SS3D.Systems.Roles;
using UnityEditor;

/// <summary>
/// Class handling the inspector display of a roleLoadout.
/// It will only show the ItemID fields if the booleans are set to true
/// </summary>
[CustomEditor(typeof(RoleLoadout))]
public class RoleLoadoutEditor : Editor
{
    private RoleLoadout roleLoadout;

    public void OnEnable()
    {
        roleLoadout = (RoleLoadout)target;
    }

    public override void OnInspectorGUI()
    {
        roleLoadout.LeftHand = EditorGUILayout.Toggle("Left Hand", roleLoadout.LeftHand);
        roleLoadout.RightHand = EditorGUILayout.Toggle("Right Hand", roleLoadout.RightHand);
        roleLoadout.LeftPocket = EditorGUILayout.Toggle("Left Pocket", roleLoadout.LeftPocket);
        roleLoadout.RightPocket = EditorGUILayout.Toggle("Right Pocket", roleLoadout.RightPocket);
		roleLoadout.LeftGlove = EditorGUILayout.Toggle("Left Glove", roleLoadout.LeftGlove);
		roleLoadout.RightGlove = EditorGUILayout.Toggle("Right Glove", roleLoadout.RightGlove);
		roleLoadout.LeftShoe = EditorGUILayout.Toggle("Left Shoe", roleLoadout.LeftShoe);
		roleLoadout.RightShoe = EditorGUILayout.Toggle("Right Shoe", roleLoadout.RightShoe);
		roleLoadout.Jumpsuit = EditorGUILayout.Toggle("Jumpsuit", roleLoadout.Jumpsuit);
		roleLoadout.Glasses = EditorGUILayout.Toggle("Glasses", roleLoadout.Glasses);
		roleLoadout.LeftEar = EditorGUILayout.Toggle("LeftEar", roleLoadout.LeftEar);
		roleLoadout.RightEar = EditorGUILayout.Toggle("RightEar", roleLoadout.RightEar);
		roleLoadout.Hat = EditorGUILayout.Toggle("Hat", roleLoadout.Hat);

		if (roleLoadout.LeftHand)
        {
            roleLoadout.LeftHandItem = (ItemId)EditorGUILayout.
                EnumPopup("Left Hand Item:", roleLoadout.LeftHandItem);
        }

        if (roleLoadout.RightHand)
        {
            roleLoadout.RightHandItem = (ItemId)EditorGUILayout.
                EnumPopup("Right Hand Item:", roleLoadout.RightHandItem);
        }

        if (roleLoadout.LeftPocket)
        {
            roleLoadout.LeftPocketItem = (ItemId)EditorGUILayout.
                EnumPopup("Left Pocket Item:", roleLoadout.LeftPocketItem);
        }

        if (roleLoadout.RightPocket)
        {
            roleLoadout.RightPocketItem = (ItemId)EditorGUILayout.
                EnumPopup("Right Pocket Item:", roleLoadout.RightPocketItem);
        }

		if (roleLoadout.LeftGlove)
		{
			roleLoadout.LeftGloveItem = (ItemId)EditorGUILayout.
				EnumPopup("Left Glove Item:", roleLoadout.LeftGloveItem);
		}

		if (roleLoadout.LeftShoe)
		{
			roleLoadout.LeftShoeItem = (ItemId)EditorGUILayout.
				EnumPopup("Left Shoe Item:", roleLoadout.LeftShoeItem);
		}

		if (roleLoadout.RightShoe)
		{
			roleLoadout.RightShoeItem = (ItemId)EditorGUILayout.
				EnumPopup("Right Shoe Item:", roleLoadout.RightShoeItem);
		}

		if (roleLoadout.RightGlove)
		{
			roleLoadout.RightGloveItem = (ItemId)EditorGUILayout.
				EnumPopup("Right Glove Item:", roleLoadout.RightGloveItem);
		}


		if (roleLoadout.LeftEar)
		{
			roleLoadout.LeftEarItem = (ItemId)EditorGUILayout.
				EnumPopup("Left Ear Item:", roleLoadout.LeftEarItem);
		}

		if (roleLoadout.RightEar)
		{
			roleLoadout.RightEarItem = (ItemId)EditorGUILayout.
				EnumPopup("Right Ear Item:", roleLoadout.RightEarItem);
		}

		if (roleLoadout.Jumpsuit)
		{
			roleLoadout.JumpsuitItem = (ItemId)EditorGUILayout.
				EnumPopup("Jumpsuit Item:", roleLoadout.JumpsuitItem);
		}

		if (roleLoadout.Hat)
		{
			roleLoadout.HatItem = (ItemId)EditorGUILayout.
				EnumPopup("Hat Item:", roleLoadout.HatItem);
		}

		if (roleLoadout.Glasses)
		{
			roleLoadout.GlassesItem = (ItemId)EditorGUILayout.
				EnumPopup("Glasses Item:", roleLoadout.GlassesItem);
		}
	}
}