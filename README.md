C# Job System Cookbook
=======================

This is a repo of examples i've written to learn how to use the C# job system to write systems at scale using the new C# job system, here for reference and sharing.  Each example script has a corresponding scene where it's set up.



## Examples
### [Accelerate 10000 Cubes](Assets/Scripts/AccelerationParallelFor.cs)

Demonstrates a simple dependency setup & working with Transforms in jobs.

### [Point & Bounds Intersection Checks](Assets/Scripts/CheckBoundsParallelFor.cs)

Demonstrates checking a `Vector3` and a `Bounds` for intersection against a list of 10000 `Bounds`.

### [Point Cloud Generation & Processing](Assets/Scripts/PointCloudProcessing.cs)

Generates a cloud of 10000 points, then calculates magnitudes & normalizes the points.
