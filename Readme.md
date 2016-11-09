#Spatial/sparse quad tree sample for C#
 
This data structure optimizes storage of big condensed filled areas.

It's suboptimal for storing very sparse non-condensed data as overhead of having multiple subnodes is quite high.

It works similar to https://www.youtube.com/watch?v=NfjybO2PIq0 except it uses lazy initialization for subnodes.

Coding: Vladimir Kozlov, AtomicTorch Studio http://atomictorch.com
