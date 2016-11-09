<h3>Spatial/sparse quad tree sample for C#</h3>
 
This data structure optimizes storage of big condensed filled areas.

It's suboptimal for storing very sparse non-condensed data as overhead of having multiple subnodes is quite high.

It works similar to https://www.youtube.com/watch?v=NfjybO2PIq0 except it uses lazy initialization for subnodes.

Download sample app (.NET 4.5) - https://drive.google.com/open?id=0B4-tSq-u4CrecjRwZXlOc3IyZms

Coding: Vladimir Kozlov, AtomicTorch Studio http://atomictorch.com
License: MIT (see License.md)
