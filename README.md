# Extended Camera Dynamics (v1.0.7)
A Celeste mod (by me) that extends camera functionality.

> Most Vanilla entities and backdrops should "just work" with this.
If I missed something, please raise an issue here or message me @LilacIsle on Discord.

### Tutorial for Map Makers:

>##### *Note:*
>By default, Extended Camera Dynamics doesn't hook into any methods until you 
load into a chapter with the correct metadata. This is because it makes a few 
breaking changes to vanilla behavior:
>	- `Level.ZoomFocusPoint` should be treated as if it doesn't exist, and does
      nothing; it's effectively nulled.
>	- Some Vanilla zooming methods (e.g.: `Level.ZoomAcross()`) are 
      effectively nulled.
>	- Some backdrops have minor visual discrepancies when compared against 
      vanilla.
>
>It can also induce a memory & performance penalty, as it has to resize 
 buffers (taking up more memory) and duplicate draw calls.

With that aside, let's create a "correct metadata" to enable 
Extended Camera Hooks in your map. 

In the folder, next to your map file, you 
need a seperate metadata file.

__Your map is here:__

`.../Maps/YOURNAME/MODNAME/MAPNAME.bin`

__Find or create this file, right next to it:__

`.../Maps/YOURNAME/MODNAME/MAPNAME.meta.yaml`

> The leading paths should be the same--they're in the same folder!

__Then, within that *.meta.yaml file, copy and paste the following into it:__

```
ExCameraMetaData:
    EnableExtendedCamera: true
    RestingZoomFactor: 1.0
```

>Make sure there's no extra indentation.
The final file might look something like this:
```
ArcherMetaData:
    ArcherSkin: 0
    CrawlMovement: true
    Inventory:
        Bow: ArcherMadeline:Athletic
Mountain:
    MountainModelDirectory: Mountain/LilacIsle/MSide/morning
    MountainTextureDirectory: LilacIsle/MSide/morning
    Idle:
        Position: [ -1.374, 1.224, 7.971 ]
        Target: [ -0.440, 0.499, 6.358 ]
    Select:
        Position: [ -1.390, 0.784, 7.593 ]
        Target: [ -0.052, 0.545, 6.125 ]
    Zoom:
        Position: [ -1.104, 0.661, 7.292 ]
        Target: [ -0.324, 0.565, 5.452 ]
    Cursor: [ -0.880595, 0.8781773, 6.77277 ]
    State: 0
    ShowCore: false
    Rotate: false
    FogColors:
      - D3F6FF
      - D3F6FF
      - D3F6FF
      - D3F6FF
    StarFogColor: D3F6FF
ExCameraMetaData:
    EnableExtendedCamera: true
    RestingZoomFactor: 1.0
```

With that, you should be done!

Of course, make sure your mod's dependencies include ExCameraDynamics.
Loenn does a pretty good job of managing these, so you shouldn't have to 
worry too much.

> ##### *Note:*
>To check if everything is working, enter your map with both your mod 
 and ExCameraDynamics loaded.
>
>Once in-game, open the console ( by pressing ~ / \` ) and type `excam_is_active`
>
>If it returns "True", then congrats! You got it! Go ahead and zoom to your 
 heart's content.
>
>If it returns "False", double-check that:
>	1) Your map file is within /Maps or a subfolder of /Maps
>	2) the metadata file exists and is in the same folder as your map file
>	3) the metadata file shares its name with your map file
>	4) the file exactly contains the provided text
>	5) there's no additional indentation within the metadata file
>
>If the game tells you that `excam_is_active` isn't a command, then make 
 sure ExCameraDynamics is loaded.

This helper mod only works with Loenn. I will only make it work with Loenn.

## Features:

 - ### CameraZoomTrigger (Trigger)
	Forces the camera to zoom in/out to fit within a specified range whenever 
    the player is inside it. Use whenever you want to zoom in or out in 
    regular gameplay.

	#### ZoomStart and ZoomEnd use zoom factors, which act as such:
	- `1.0` => vanilla zoom
		(default)

	- `0.5` => the game world is rendered at half scale : you can see 2x as much
	           in both directions. (zoomed out)

	- `2.0` => the game world is rendered at double scale: you can see half as
	           much in both directions. (zoomed in)
    
	#### IsMax: `bool`
	 
	>If true, then this trigger specifies the maximum zoom factor (minimum zoom in) of an area.
	>
	>If false, then this trigger specifies the minimum zoom factor (maximum zoom out) of an area. 

	#### Mode: `Enum`
	
	>	Start: 
	>		Sets the min/max zoom factor to the value of ZoomStart
	>
	>	LeftToRight:
	>		As the player moves from left to right, linearly interpolate between ZoomStart and ZoomEnd.
	>		Sets the min/max zoom factor to that interpolated factor.
	>
	>	RightToLeft, BottomToTop, TopToBottom: The above, but for the different directions.
	
	> ##### *Notes:*
	> - The game will not resize the buffers beyond zoom factor 0.125 (dimensions 2560x1440)- 
	    This renders pixel-perfect at 1-1 scale for 1440p monitors. 
	    Yes, this is because *I* have a 1440p monitor, and I wanted to see what
	    that looked like. Yes, I would have made the upper limit be 1-1 for 
	    4K (3840x2160), but Madeline is so small at that point that you really 
	    can't play the game anyways.
	>
	> - I would recommend never dipping below `0.25f`- This renders 
	    pixel-perfect at 1-1 scale for 720p monitors, and any lower will make 
	    some things lose visual clarity or even disappear at that resolution. 
            I don't think anyone really wants 720p on their desktop computers 
            anymore, but it is basically the resolution on the Steam Deck (1280x800)
            and I'm sure there are some 720p laptop users who would really 
            appreciate it if mappers didn't make parts of the game lose significant
            detail while performing worse.
	>
	> - Pixel-perfect for 1080p is `0.16666666...`  == (1/6)

 - ### CameraReferenceFrame (Entity)
	Specifies a camera position and zoom in the level. Used for a few other 
    things. You can identify them with the "EasyKey" parameter.

	> ##### *Notes:*
	> I used this string-based system instead of EntityID as they are 
          designer-named and more human-readable.

 - ### ReferenceFrameLookout (Entity)
	A pathed watchtower that uses CameraReferenceFrames as nodes to zoom & move
    between. The "EasyKeys" parameter is a comma-seperated list of each frame
    the lookout interpolates between, in sequence.

	> ##### *Notes:*
	> This was adapted from vanilla code, so the lookout doesn't actually go 
	  through each CameraReferenceFrame exactly. It *does* interpolate smoothly...
	  but also goes right past them. Adjust the frames' positions and zooms until 
	  you get the motion about where you want.
	
 - ### CameraFocusTarget (Component)
	A component that influences the camera towards its entity with a weight.
	You can specify the maximum amount (minimum factor) that an area can zoom 
    out to with a CameraZoomTrigger.

	The game will take the weighted average of all the target positions and try 
	to focus around that point.
	> The player is mathematically considered to be a target with Weight == `1f`.
	> However, the player does not actually have a CameraFocusTarget component.
	
	The camera will zoom out to try and fit all of the targets if they are not 
	already on screen. The game will always clamp the camera so that the player 
	is never too close to the edge of the screen.

	> ##### *Notes:*
	> I've used this for anything that's both important and mobile-- although 
	  I admit that for my use-case it's basically just bosses.

 - ### ZoomParallaxDepth (Backdrop)
	A parallax backdrop that uses a perspective interpretation to scroll
    and scale. Instead of a digital zoom, the camera looks like it moves 
    closer / further away.

	The camera's z-coordinate is calculated as `(-1f / level.Zoom)`.
	The game area is at z-coordinate `0f`.

	Perfect for making the foreground/background look like they have a 3D 
	position as the camera zooms in/out.

	> ##### *Notes:*
	> I've found that this effect works particularly well for foregrounds.

- ### ZoomParallax (Backdrop)
	A parallax backdrop that uses screen-space-based scaling to scale / move 
    as the camera pans & zooms. The "Scale Retention" parameter determines how 
    much screen-space it retains as the camera zooms.

	>	`1.0` => retains its scale proportional to the screen regardless of zoom
	>
	>	`0.0` => retains its scale proportional to the game world regardless of zoom
	>
	>	`0.5` => ... I'm not explaining this, and I haven't found a use for this.
	>
	>   All other values are some in-between.

	Perfect for backdrops that are tied to the camera and backdrops that are 
	'infinitely' far away.
		e.g.: a vignette and the stars, respectively.

	> ##### *Notes:*
	> I've personally found that best use-cases for this are at 
	  ScaleRetention == `1f`. Other values could be useful, so I've left the 
	  option in. Maybe someone will find them useful.
	>
	> And no! You don't need this texture to be (320x180)- you can use
	  `BaseScale` to scale down a higher-resolution image. However, it's worth
	  noting that if you use a high-resolution texture (realistically no 
	  larger than 2560 x 1440), the texture needs to be manually offset back
	  to the top-left corner of the screen. This is just a byproduct of the 
	  way parallax is implemented.
	> Using values:
	```
    	X = 160 - (Texture width) * 0.5
    	Y = 90 - (Texture height) * 0.5
	```
	> in their respective fields should 're-calibrate' the image.
	  Doing this will give you a backdrop that takes up the whole screen,
	  and will still render at full resolution when zoomed out.

 - ### ZoomParticleDepth (Backdrop)
	A particle-based backdrop that uses a perspective interpretation to 
    scroll and scale. Instead of a digital zoom, the camera looks like it 
    moves closer / further away.

	The camera's z-coordinate is calculated as `(-1f / level.Zoom)`.
	The game area is at z-coordinate `0f`.

	Particles will disappear as the camera moves 'in front' of them, and are
	sorted from back to front. Perfect for making particles look like they are
	spaced throughout a 3D volume as the camera zooms in/out.
	
	The particle update method is marked as virtual if you want to more easily
	make custom particle behavior.

	> ##### *Notes:*
	> I love how effectively this sells the volume of a space.

- ### ZoomParticleParallax (Backdrop)
	A particle-based backdrop that uses an orthogonal interpretation to scroll
    and scale. (Uses  `ScrollX` / `ScrollY`) Perfect if you want to have 
    vanilla-style particles that loop & cull correctly with respect to the
    camera zoom.

	The particle update method is marked as virtual if you want to more easily
	make custom particle behavior.

	> ##### *Notes:*
	> This was made after I finished patching the 'Planets' backdrop.
	  The logic behind it was also used for a few others (like 'Petals')
	  So this exists more-or-less as a more customizable 'generic' version of 
	  those backdrops.

- ### Focus Camera Command (Dialogue function)
	Use this in your dialog files:

	`{ focus_camera <EasyKey:string> <Duration:float> }`
	
	And the camera will zoom to a CameraReferenceFrame with a matching EasyKey.
	
	Takes Duration seconds to reach the frame.

	You can also use:

	`{ reset_camera }`

	To let the camera revert itself automatically.

- ### Console Commands: 
	- `excam_is_active`
	   > Tells you if the camera hooks are currently active.
	- `excam_enable_hooks`
	   > Forcefully enables camera hooks for levels without the metadata.
	- `excam_force_zoom <factor:float>`
	   > Forces the zoom factor to the specified value. Negative values negate its effect.
	- `excam_zoom_to_reference_frame <EasyKey:string> <Duration:float>`
	   > Zoom & pan over to the CameraReferenceFrame with matching Easykey over 
        Duration seconds. The camera will be stuck afterwards. Call 
        `excam_unstick_zoom` to unstick it.
	- `excam_unstick_zoom`
	   > Some manual camera manipulation methods disable the automatic zooming
             of the camera for their duration. This means the camera will be 
             offset & funky after some cutscenes if the cutscene doesn't reset
             the zoom. Use this to re-enable the automatic zooming and return to 
             regular behavior.
	- `excam_set_resting_zoom`
	   > Sets the default zoom to the specified factor. Negative values reset to the value specified in the Chapter's metadata.
	- `excam_set_snap_speed`
	   > Multiplies the camera's interpolation by the specified factor. Negative values reset to the value specified in the Chapter's metadata.

## Oh! also!!

I pre-emptively give any and all permission I legally have to let anyone yoink 
this whole repo and use it for anything related to Celeste / modding Celeste--
it's too specialized to be useful to anything else, probably. I'd rather see 
the cool stuff people could do with it.

Just if you do, give credit. A url to this repo would be more than sufficient. 
Thanks, and happy modding!
