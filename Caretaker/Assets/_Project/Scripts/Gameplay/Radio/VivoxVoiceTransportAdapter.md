# Vivox VoiceTransport Adapter Flow

This file is a placeholder note for the backup Vivox path. It is intentionally Markdown, not C#, because the Vivox package is not installed in this project yet.

## Goal

Implement `IVoiceTransport` with Vivox while keeping `RadioNetworkBridge` as the single source of truth for the PTT lock.

## Flow

1. Initialize Unity Gaming Services.
2. Authenticate the local player.
3. Initialize and sign in to Vivox.
4. Join one shared 2-player voice channel for the session.
5. Keep Vivox local transmission muted by default.
6. `RadioInputController` requests talk lock from `RadioNetworkBridge`.
7. When local state becomes `Transmitting`, call Vivox local mute/unmute API to allow microphone transmission.
8. When local state becomes `Idle`, `Receiving`, or `Blocked`, mute local Vivox transmission again.

## Rule

Vivox should not decide who is allowed to talk. It only carries voice after `RadioNetworkBridge` grants the lock.
