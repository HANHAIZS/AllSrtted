# Project Architecture Rules

## QFramework roles

| Responsibility | Owner | Typical content |
| --- | --- | --- |
| State mutation | Command | User action, flow transition, setting update |
| Persistent or observable data | Model | Runtime state, value objects, `BindableProperty<T>` |
| Reusable project logic | System | Rules and orchestration shared by multiple controllers |
| One-shot notification | Event | Completed, failed, opened, closed |
| Unity presentation | ViewController | Input forwarding, visual binding, animation trigger |
| Stateless adapter/helper | Utility | Repository, platform bridge, formatter |

Use one project-owned `Architecture<T>` and one project Controller base. Foundation modules register services into that architecture; they never replace it.

## Dependency access

- Models and Systems cache required dependencies in `OnInit`.
- MonoBehaviours cache them in `Awake`, `OnInit`, or the established binding phase.
- Commands resolve each dependency into a local variable before invoking it.
- Runtime callbacks, refresh methods, animations, and analytics helpers do not repeatedly call `GetModel`, `GetSystem`, or `GetUtility`.
- After editing, search changed files for those three calls and inspect every remaining location.

## Folder shape

Centralize role folders at the project code root. Add responsibility subfolders inside a role, for example `Models/Settings`, `Systems/Startup`, or `ViewControllers/UI`. Do not create feature folders that each contain their own role tree.
