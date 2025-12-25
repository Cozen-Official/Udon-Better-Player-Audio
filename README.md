# Udon-Better-Player-Audio
The Better Player Audio component is an udon script that adjusts the far distance and voice gain of remote players based on the number of players in the instance.
This helps with small or crowded vrchat worlds where many players may be talking in close proximity. 

The values interoplate between the Start and End values as player count changes between Min Player and Max Player thresholds. Supports up to 100 player instances (unusual).
You can set Start and End values for both Far Distance and Voice Gain.
You can also set the voice low pass filter (default on).
Nine interpolation curves are available: linear, ease-in, ease-out, ease-in-out, smoothstep, smootherstep, exponential, diminishing, and log like.
Each curve affects the interpolation between the Start and End values.
You can optionally set a check interval to force update at a specified interval (in seconds). This shouldn't be needed, as it works on join/leave events, but the check can help if those events are missed for some reason or another.
The custom editor displays the curve and settings in real time so you can visually see how the audio settings will be adjusted as player count changes.


<img width="382" height="588" alt="image" src="https://github.com/user-attachments/assets/8afecc98-2cde-4761-a496-6de4180dec6f" />

Setup:
Import the package.
Add the "Better Player Audio" component on any active gameobject in your scene.
Adjust settings as desired.
Profit.

Contact "cozen." on Discord for support or join [My Discord](https://discord.gg/ZWRWCgnE3P)
