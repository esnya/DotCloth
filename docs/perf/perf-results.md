# Performance Results

Each run advances 200 steps at dt=0.016 using the semi-implicit integrator. Metrics are `total/frame/FPS`.

```
cloth      | Springs           | Shells            | FEM               | Springs+Strain
-----------+-------------------+-------------------+-------------------+----------------
10x10x1    | 35.7/0.18/5607.2  | 14.3/0.07/13939.1 | 23.2/0.12/8613.7  | 8.4/0.04/23734.1
10x10x256  | 526.2/2.63/380.1  | 882.2/4.41/226.7  | 931.8/4.66/214.6  | 594.9/2.97/336.2
20x20x256  | 1341.3/6.71/149.1 | 2281.2/11.41/87.7 | 2549.8/12.75/78.4 | 1791.3/8.96/111.7
30x30x256  | 2278.5/11.39/87.8 | 5924.4/29.62/33.8 | 6048.7/30.24/33.1 | 2939.9/14.70/68.0
```

Environment: Ubuntu 24.04.2 LTS on Intel Xeon Platinum 8272CL @ 2.60GHz, 5 vCPUs (nproc), 4 logical cores, ThreadPool max 32767, .NET 9.0.9.

