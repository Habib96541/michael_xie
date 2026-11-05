namespace BullRide.Core
{
    internal static class BuckMoves
    {
        public static readonly BuckMove[] Sequence =
        {
            new BuckMove("BLOW OUT", new BuckKey[]
            {
                new BuckKey(0, .01f, 4.8f, .03f, .00f, .02f, .02f, .02f, .114f, .033f),
                new BuckKey(.32f, .11f, 5.4f, -.17f, -.10f, .15f, -.15f, .28f, .177f, .063f),
                new BuckKey(.68f, .25f, 4.3f, .33f, -.16f, .93f, .23f, .37f, .231f, .054f),
                new BuckKey(.99f, .09f, 3.5f, .01f, -.05f, .00f, .04f, .00f, .15f, .012f),
                new BuckKey(1.29f, -.1f, 4.7f, -.23f, -.15f, .10f, -.17f, .34f, .195f, .063f),
                new BuckKey(1.66f, .2f, 3.9f, .28f, -.19f, .82f, .22f, .45f, .213f, .066f),
                new BuckKey(2.04f, .0f, 3.1f, .02f, -.04f, .00f, .04f, .00f, .135f, .063f),
                new BuckKey(2.23f, .0f, 3.2f, .02f, .00f, .00f, .00f, .00f, .099f, .087f),
            }),
            new BuckMove("DROP KICK", new BuckKey[]
            {
                new BuckKey(0, .0f, 3.2f, .02f, .00f, .00f, .00f, .00f, .145f, .128f),
                new BuckKey(.27f, .27f, 2.6f, -.26f, -.05f, .08f, -.10f, .26f, .114f, .264f),
                new BuckKey(.57f, .34f, 2.7f, .43f, -.12f, 1.0f, .27f, .48f, .172f, .422f),
                new BuckKey(.92f, .1f, 2.6f, .10f, -.10f, .24f, .10f, .13f, .141f, .339f),
                new BuckKey(1.16f, -.2f, 2.5f, -.13f, -.02f, .00f, -.12f, .00f, .114f, .185f),
                new BuckKey(1.46f, -.3f, 3.7f, .36f, -.14f, .88f, .30f, .34f, .158f, .392f),
                new BuckKey(1.83f, -.1f, 2.5f, .04f, -.06f, .05f, .05f, .02f, .176f, .304f),
                new BuckKey(2.12f, .0f, 2.7f, .00f, .00f, .00f, .00f, .00f, .189f, .189f),
            }),
            new BuckMove("SPIN", new BuckKey[]
            {
                new BuckKey(0, -.3f, 2.6f, .04f, .02f, .00f, .02f, .00f, .22f, .12f),
                new BuckKey(.25f, -1.65f, 2.8f, -.18f, .15f, .15f, -.11f, .24f, .76f, .21f),
                new BuckKey(.65f, -2.65f, 2.5f, .25f, .27f, .68f, .15f, .42f, .99f, .06f),
                new BuckKey(1.02f, -2.85f, 2.0f, -.16f, .23f, .21f, -.11f, .17f, .88f, -.22f),
                new BuckKey(1.38f, -2.25f, 2.8f, .20f, .23f, .65f, .17f, .34f, .93f, .15f),
                new BuckKey(1.76f, -.75f, 2.7f, .02f, .05f, .00f, .00f, .00f, .39f, .20f),
            }),
            new BuckMove("SPIN", new BuckKey[]
            {
                new BuckKey(0, .3f, 2.6f, .04f, -.02f, .00f, .02f, .00f, -.22f, .12f),
                new BuckKey(.27f, 1.85f, 2.8f, -.18f, -.15f, .15f, -.11f, .24f, -.76f, .21f),
                new BuckKey(.67f, 2.75f, 2.5f, .25f, -.27f, .68f, .15f, .42f, -.99f, .06f),
                new BuckKey(1.03f, 2.85f, 2.0f, -.16f, -.23f, .21f, -.11f, .17f, -.88f, -.22f),
                new BuckKey(1.39f, 2.25f, 2.8f, .20f, -.23f, .65f, .17f, .34f, -.93f, .15f),
                new BuckKey(1.79f, .75f, 2.7f, .02f, -.05f, .00f, .00f, .00f, -.39f, .20f),
            }),
            new BuckMove("REVERSE", new BuckKey[]
            {
                new BuckKey(0, .20f, 2.6f, .01f, .00f, .00f, .00f, .00f, -.17f, .24f),
                new BuckKey(.29f, .75f, 2.3f, -.12f, .13f, .24f, -.05f, .26f, -.65f, .54f),
                new BuckKey(.64f, -2.50f, 1.2f, .33f, -.24f, .89f, .26f, .45f, .89f, -.49f),
                new BuckKey(1.02f, -2.3f, 2.2f, -.23f, -.14f, .12f, -.16f, .18f, .67f, -.30f),
                new BuckKey(1.49f, -.32f, 2.9f, .09f, .02f, .09f, .03f, .03f, .26f, .25f),
            }),
            new BuckMove("REAR", new BuckKey[]
            {
                new BuckKey(0, .0f, 2.5f, .02f, .00f, .00f, .00f, .00f, .11f, -.16f),
                new BuckKey(.35f, -.41f, 1.7f, -.35f, .08f, .03f, -.16f, .18f, .34f, -.78f),
                new BuckKey(.75f, -.46f, 1.2f, -.51f, .06f, .09f, -.19f, .23f, .25f, -.96f),
                new BuckKey(1.13f, .3f, 2.5f, .28f, .11f, .66f, .27f, .44f, -.26f, .78f),
                new BuckKey(1.54f, .3f, 3.2f, .02f, .00f, .00f, .00f, .00f, -.18f, .37f),
            }),
            new BuckMove("BODY ROLL", new BuckKey[]
            {
                new BuckKey(0, .04f, 2.9f, .02f, .00f, .00f, .00f, .00f, .17f, .25f),
                new BuckKey(.25f, -1.08f, 2.9f, -.19f, -.30f, .10f, -.10f, .29f, .82f, -.19f),
                new BuckKey(.57f, -1.58f, 3.1f, .20f, -.43f, .70f, .17f, .50f, .98f, .31f),
                new BuckKey(.92f, .64f, 2.7f, .24f, .28f, .73f, .15f, .26f, -.72f, .30f),
                new BuckKey(1.31f, .95f, 2.4f, -.11f, .36f, .15f, -.12f, .19f, -.90f, -.13f),
                new BuckKey(1.67f, .31f, 2.9f, .03f, .06f, .00f, .03f, .00f, -.30f, .18f),
            }),
            new BuckMove("SIDE TO SIDE", new BuckKey[]
            {
                new BuckKey(0, .0f, 2.6f, .01f, .00f, .00f, .00f, .00f, -.13f, .10f),
                new BuckKey(.27f, .96f, 3.1f, -.18f, .24f, .22f, -.14f, .23f, -.88f, .12f),
                new BuckKey(.61f, -1.0f, 2.9f, .25f, -.28f, .62f, .20f, .32f, .88f, .24f),
                new BuckKey(.93f, 1.1f, 3.1f, -.15f, .27f, .23f, -.08f, .21f, -.94f, -.25f),
                new BuckKey(1.28f, -.9f, 3.0f, .24f, -.24f, .64f, .17f, .35f, .91f, .32f),
                new BuckKey(1.69f, -.3f, 2.7f, .03f, -.03f, .00f, .04f, .00f, .30f, .19f),
            }),
            new BuckMove("STUTTER KICK", new BuckKey[]
            {
                new BuckKey(0, .08f, 3.2f, .01f, .00f, .00f, .00f, .00f, .11f, .20f),
                new BuckKey(.22f, .69f, 1.7f, .32f, -.13f, .87f, .17f, .26f, .43f, .89f),
                new BuckKey(.42f, .43f, 2.8f, -.23f, -.09f, .07f, -.13f, .08f, .46f, -.25f),
                new BuckKey(.64f, -.45f, 1.7f, .35f, .14f, .93f, .21f, .31f, -.40f, .91f),
                new BuckKey(.87f, -.59f, 2.9f, -.24f, .11f, .11f, -.15f, .08f, -.49f, -.32f),
                new BuckKey(1.12f, .61f, 2.5f, .31f, -.14f, .86f, .25f, .34f, .42f, .88f),
                new BuckKey(1.47f, .22f, 2.8f, .02f, -.02f, .00f, .00f, .00f, .29f, .25f),
            }),
        };
    }
}
