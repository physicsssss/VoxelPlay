This demo scene uses a custom terrain generator class (located in the Sources folder).
The goal is to show how you can create your own terrain generator class completely from scratch.

The process to create your own generator is described here:
https://kronnect.freshdesk.com/solution/articles/42000001950-coding-your-own-terrain-generator

Basically it involves 3 steps:
- Create a new class that derives from VoxelPlayTerrainGenerator. This script will include all the code neccessary to fill the chunk contents.
- Create a new ScriptableObject from that new class. This object can contain custom parameters exposed by the public properties of the new generator class (for example your custom terrain generator could allow to specifiy the altitude or voxel definitions that can be used).
- Assign the ScriptableObject to your World in the scene. Once you assign the ScriptableObject to the World, it will be used to populate the chunks when they're required.


Contact us at contact@kronnect.me or on our support forum for any question
www.kronnect.com

Thanks and enjoy!
