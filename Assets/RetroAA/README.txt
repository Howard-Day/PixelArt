The RetroAA shader looks like point filtering, but without the aliasing. For it to work, you have to choose one of the following texture import settings:

Good: Filter Mode Bilinear.
Better: Filter Mode Trilinear.
Best: Filter Mode Trilinear with Aniso Level > 0 (more is better).

On a mobile device, you'll probably want to stick with Bilinear for best performance. On a Desktop or console, you can go for maximum quality.