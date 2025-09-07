# Solver stiffness scaling

## Purpose
Ensure solver behaviour remains predictable as stiffness values approach zero.

## Summary
- Apply minimum stiffness floors when mapping to constraint softness and clamps.
- Execute post-stabilization based on constraint presence instead of stiffness thresholds.

## Testing
- Parameter sweep for stretch, bend, and tether across low and high values.
- `dotnet format --check`
- `dotnet build -f net9.0`
- `dotnet test -f net9.0`
- `dotnet build -f net8.0`
- `dotnet test -f net8.0`
- `dotnet build -f net9.0 --property DotClothEnableExperimentalXpbd=true`
- `dotnet test -f net9.0 --property DotClothEnableExperimentalXpbd=true`
- `dotnet build -f net8.0 --property DotClothEnableExperimentalXpbd=true`
- `dotnet test -f net8.0 --property DotClothEnableExperimentalXpbd=true`
