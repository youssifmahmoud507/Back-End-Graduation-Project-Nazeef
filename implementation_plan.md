# AI Voice Customer Support Implementation

We will create a real-time voice and video customer support component using the newly announced Gemini 3.1 Flash Live Preview model and the `google-genai` SDK over WebSockets. This will allow low-latency audio-to-audio dialogue, tool calling (like Grok), and vision capabilities via the front camera.

## User Review Required

> [!IMPORTANT]  
> We need to install the new `@google/genai` library via npm to interface with the Gemini Live API. Is it okay if I proceed with installing this package? Also, do you have a Google Gemini API Key that you are ready to put in your `.env.local` file?

> [!WARNING]
> Accessing the camera and microphone in the browser requires HTTPS (or `localhost`). I will implement the standard `navigator.mediaDevices.getUserMedia` for this. Since video scanning and audio capture require maintaining high performance, we'll send frames continuously and small PCM audio chunks as the documentation indicates.

## Proposed Changes

### Frontend Components

#### [NEW] src/app/components/LiveSupportWidget.tsx
A new floating widget or full page section that acts as the customer support interface.
- Includes a button to start the call.
- Renders the video feed from the front camera locally (to show what the AI is seeing).
- Handles incoming PCM audio and plays it via Web Audio API.

#### [NEW] src/app/hooks/useGeminiLive.ts
A custom hook that encapsulates the Gemini Live WebSocket connection using `@google/genai` or direct WebSocket if building raw in the browser.
- Manages connection lifecycle.
- Processes audio input via AudioWorklet for sending 16kHz PCM audio chunks.
- Processes video frames via hidden canvas and sends them as JPEG blobs at 1 FPS.
- Registers standard laundry-related tools (like `track_order`, `get_pricing`).

#### [MODIFY] src/app/pages/Login.tsx or similar
We will integrate the generic `LiveSupportWidget` on an accessible page (like the landing page or a dedicated support page) so we can test it seamlessly.

## Open Questions

- What specific "tools" (function calls) would you like the AI to support out of the box? Examples might include checking laundry service prices, tracking an order status, or getting help with an issue.
- Would you prefer this to be a floating "chat/voice" bubble on all pages, or a dedicated `/support` page for this test?

## Verification Plan

### Manual Verification
1. I will run or build the app.
2. Open the page and click the "Start Support Call" button.
3. Allow browser permissions for Camera and Microphone.
4. Speak to the AI and test an edge case (like asking for an order status) to ensure tool calling triggers properly and it replies back in voice.
5. Wave at the camera and ask the AI "What am I holding?" to verify video access.
