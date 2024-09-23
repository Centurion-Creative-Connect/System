# スコープを設定する

To set up scope for the gun variant, First you will need to

- Find an appropriate shader for scope
- Create or find the scope that you are going to configure for the gun variant.

## Configuring scopes for the gun

Most scope shaders requires one camera that is capturing from scope to render a RenderTexture. Because the
RenderTextures are shared between gun instances, one can overwrite others RenderTexture and can corrupt the view. To
mitigate, Create an AnimationClip that toggles the scope camera, and apply it to the `LOCAL_ON_PICKUP` and
`LOCAL_ON_DROP` clip.