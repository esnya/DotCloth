# Stiffness Mapping and Adaptive Softness

## Purpose
Ensure cloth parameters respond smoothly to user input and reduce abrupt changes when stiffness approaches zero.

## Design
- Map user stiffness $s\in[0,1]$ to Baumgarte factor using
  \[\beta = s\,\beta_{max}\] with $\beta_{max}=0.7$.
- Scale softness (CFM) and per-iteration impulse clamps linearly with $s$:
  - $\text{cfm} = \frac{\text{base}}{s+10^{-3}}$
  - $\text{clamp} = \text{base}\cdot s$
- Always evaluate stretch and bend constraints; zero stiffness simply yields $\beta=0$ and large softness.

## Testing Strategy
Regression tests and manual CLI comparisons ensure parameter changes alter behaviour and bounds remain tight.
