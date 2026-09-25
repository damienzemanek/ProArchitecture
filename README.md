# Pro Architecture
Procedural Architectural System for Unity in C#
- Data-Oriented-Design (DOD)
- High Performance
- Zero GC allocations
- Retains ergonomics of Unity Workflows

Key Design Pillars:
- Unmanaged & Blittable State: Data lives in flat, contiguous unmanaged structs `Data<T, TMetaData>`, eliminating GC churn during update loops
- Raw Function Pointer Execution: Logic is decoupled into atomic `Operation<T>` units invoked via raw C# function pointers `delegate*`, avoiding delegate allocations and VTable lookup overhead.
- Managed to Unmanaged Bridging: `BlittableReference<T>` wraps `GCHandle` targets into pointer sized blittable structs, allowing unmanaged data to safely reference managed Unity components
- Lifecycle & Domain-Reload Safety: `DataActiveRegistry` and `MallocHandle<T>` track persistent unmanaged allocations and cleans them up automatically during editor domain reloads preventing memeory leaks.
- Composible Logic & Operation Grouping: `Operation<T>`s are grouped into `Logic<T>`s which are further grouped into `LogicGroup<T>`s, Supports both `Static` and `Dynamic` Logic and Logic Groups with fluent API builders
- Fluent API, Zero Allocation Predicates: Fast boolean logic evaluation gates that support chaining `.And()`, `.Or()` and bitwise negation with Zero GC Churn due to static lifecycle.


## Test Cases

### Pro Architecture Core
<img width="605" height="623" alt="ProArchTests" src="https://github.com/user-attachments/assets/09b91c4e-ca81-49c6-a9b2-c32b58c4a1c2" />

### ProSM

<img width="603" height="805" alt="ProSMTests" src="https://github.com/user-attachments/assets/0127c98c-8be3-4a64-80c1-6e3a6d218665" />
