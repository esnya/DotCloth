API Overview
============

DotCloth currently provides a patent-neutral mass–spring solver with pluggable Euler integrators (semi-implicit by default). The API mirrors common Unity Cloth parameters while keeping modules swappable.

Parameter Mapping
-----------------
| Unity Cloth Parameter | MassSpringCloth Parameter |
|-----------------------|---------------------------|
| Stretching Stiffness  | Spring.Stiffness          |
| Damping               | damping                   |
| Use Gravity           | gravity                   |
| Mass                  | 1 / invMass               |

Additional parameters such as friction or wind can be layered on later via new force models or colliders.
