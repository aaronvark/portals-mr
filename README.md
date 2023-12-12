[![MIT License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat)](http://choosealicense.com/licenses/mit/)

PortalsMR includes functioning portals in Mixed Reality (MR) for Unity, and is adapted from PortalsVR by [Daniel Lochner: VR Natural Walking in Impossible Spaces](https://daniellochner.itch.io/impossible-spaces-vr)., which itself derives from [Portals](https://github.com/SebLague/Portals) (by Sebastian Lague) and [PocketPortalVR](https://github.com/andrewzimmer906/PocketPortalVR) (by Andrew Zimmer). It was made specifically for my research project on Portals in Mixed Reality contexts

Important deviations from Lochner's implementation:

> ### 1. Mixed Reality:
> Portals provide a view into a different virtual layer, but no longer teleport (since the user cannot be teleported in Mixed Reality). Rendering still happens largely as in Lochner's version, but a special setup using MultiView & Per-Eye cameras have been implemented to be able to make use of the Meta Quest passthrough feature. The MultiView nature means scene flags cannot differ between renders of each eye, and thus the portal shader has been adapted to discard pixels that are viewed on the incorrect eye (instead of each eye seeing a different set of objects). Portals also have support for transparency, as in some cases it is desirable to both be able to see what virtually lies beyond a portal (for instance when it is immersive), whilst still being able to know what is behind it physically. Whatever the setting, the portal becomes opaque once you approach it to prevent any non-seamless visual interaction.
>
> ### 2. Teleportation:
> This should in theory still be possible, but some optimizations to the rendering have currently broken the display of this feature. To use this feature, simply make an object a "PortalTraveller", and set it up correctly by using its "Migrate" option and disabling the deformation feature. This is still WIP (it worked at one point, but has currently broken, so will be fixed again in future).
>
> ### 3. Worlds:
> The worlds that were present as mere hierarchy in the original project are now integral to how the whole thing works. Each World keeps track of renderers that are part of its environment. Anything not currently necessary to see, is set to a specific layer (30). Physics are currently exempt from this, but should in future also be handled in a similar fashion (to prevent separate worlds from intercolliding). It is also possible to have objects outside of a World, and those objects become "global", as in they are present in all worlds. This can be useful for mixed-reality objects that adhere to real objects such as doorposts. Dynamic objects can also be migrated from one world to another through the Migrate function that is part of the World class, and it can be optionally re-parented if necessary.
>
> ### 4. Passthrough pancake:
> The passthrough is separate into an overlay and underlay layer that can be minipulated on a per-world basis. Any changes happen as seamlessly as possible, but it is currently not possible to visually recurse portals, or to have a separately active view of passthrough visible through a portal.
>
> ### 5. Known issues:
> There is a bug in the OVRCameraRig.cs script when using per-eye cameras (necessary for using render textures in VR, no other combination of settings have been shown to be viable with Meta Quest). The following if-statement should be found and altered as below:
```
if (!hmdPresent || monoscopic)
{
    leftEyeAnchor.localPosition = centerEyeAnchor.localPosition;
    rightEyeAnchor.localPosition = centerEyeAnchor.localPosition;
    leftEyeAnchor.localRotation = centerEyeAnchor.localRotation;
    rightEyeAnchor.localRotation = centerEyeAnchor.localRotation;
}
else
{
	// This creates an issue when using per-eye cameras, so just do the same as normal
	leftEyeAnchor.localPosition = centerEyeAnchor.localPosition;
	rightEyeAnchor.localPosition = centerEyeAnchor.localPosition;
	leftEyeAnchor.localRotation = centerEyeAnchor.localRotation;
	rightEyeAnchor.localRotation = centerEyeAnchor.localRotation;
				
    /*
    Vector3 leftEyePosition = Vector3.zero;
    Vector3 rightEyePosition = Vector3.zero;
    Quaternion leftEyeRotation = Quaternion.identity;
    Quaternion rightEyeRotation = Quaternion.identity;

    if (OVRNodeStateProperties.GetNodeStatePropertyVector3(Node.LeftEye, NodeStatePropertyType.Position,
            OVRPlugin.Node.EyeLeft, OVRPlugin.Step.Render, out leftEyePosition))
        leftEyeAnchor.localPosition = leftEyePosition;
    if (OVRNodeStateProperties.GetNodeStatePropertyVector3(Node.RightEye, NodeStatePropertyType.Position,
            OVRPlugin.Node.EyeRight, OVRPlugin.Step.Render, out rightEyePosition))
        rightEyeAnchor.localPosition = rightEyePosition;
    if (OVRNodeStateProperties.GetNodeStatePropertyQuaternion(Node.LeftEye,
            NodeStatePropertyType.Orientation, OVRPlugin.Node.EyeLeft, OVRPlugin.Step.Render,
            out leftEyeRotation))
        leftEyeAnchor.localRotation = leftEyeRotation;
    if (OVRNodeStateProperties.GetNodeStatePropertyQuaternion(Node.RightEye,
            NodeStatePropertyType.Orientation, OVRPlugin.Node.EyeRight, OVRPlugin.Step.Render,
            out rightEyeRotation))
        rightEyeAnchor.localRotation = rightEyeRotation;
    */
}
```
