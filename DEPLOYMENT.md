# Deployment

Live game: https://chicken-rush-classy-cross-roads.am-ome.workers.dev

Unity project is at the repository root. The committed playable build is in Builds/WebGL/.
Open the project with Unity 6000.3.10f1 and Web Build Support.

Deploy the included build from this repository root using:

    npx wrangler deploy

Cloudflare Workers Static Assets serves only Builds/WebGL; Unity source and QA files are not public site assets.
The live player was loaded in Chrome at 390x844 with emulated touch: Menu state, portrait layout, no text overflow or browser errors.
Physical iPhone/Safari validation remains outstanding.
