# One test seam: a pure Session state machine under an MVVM shell

We decided the app's behavior — playback control, Scene ring navigation, Sleep Timer countdown and fades, Interruption policy, master volume, Night accrual and persistence — lives in a single platform-agnostic Session module (plain C#, no MAUI or MediaElement types, injected clock and output port), driven via intents and observed via state changes. The UI follows MVVM with dependency injection: thin ViewModels translate user interaction into Session intents and render Session state; a thin MediaElement/graphics adapter implements the output port and forwards platform events (interruptions, device disconnects) as intents. Tests target only the Session seam with a fake clock and fake output port; adapters stay too thin to test.

## Considered Options

- Testing at the ViewModel level: rejected — couples tests to MAUI's types for no behavioral gain, since ViewModels should hold no logic.
- Testing on-device (UITest/TestFlight): rejected as the primary harness — too slow for state-machine logic like timer expiry and Interruption policy.

## Consequences

- Any temptation to put logic in ViewModels or the platform adapter must be resisted; if a VM accumulates `if`s, that logic belongs in Session.
- All time-dependent behavior must flow through the injected clock so fades, countdown expiry, and the 10-minute Night threshold are testable without waiting.
