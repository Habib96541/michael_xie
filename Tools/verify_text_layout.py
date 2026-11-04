"""Regression checks for labels disappearing in short UI rows."""
from pathlib import Path
import json
from playwright.sync_api import sync_playwright

root = Path(__file__).resolve().parents[1]
out = root / "SprintDoc" / "Tests"
sizes = [(1914,909),(2560,1080),(800,360),(1920,1080),(360,800),(720,1280),(768,1024),(1280,720),(1600,900)]
report = []

def wait_labels(page, expected):
    page.wait_for_function("""expected => {
        const labels = window.blitzTelemetry?.Labels;
        return labels && expected.every(word => labels.some(label => label.Text.includes(word)))
            && labels.every(label => label.Vertices > 4 && label.Characters >= label.Text.split('\\n').join('').split('\\r').join('').length);
    }""", arg=expected, polling=100, timeout=6000)
    labels = page.evaluate("window.blitzTelemetry.Labels")
    assert all(label["Vertices"] > 4 for label in labels), labels
    assert all(label["Characters"] >= len(label["Text"].replace("\n", "").replace("\r", "")) for label in labels), labels
    assert all(label["Left"] >= 0 and label["Right"] <= 1.001
        and label["Bottom"] >= 0 and label["Top"] <= 1.001 for label in labels), labels
    return labels

def action(page, row):
    width = page.viewport_size["width"]
    height = page.viewport_size["height"]
    y = .02 + (1 - (.4225 - row * .09)) * .96
    page.mouse.click(width*.5, height*y, delay=75)
    page.wait_for_timeout(150)

with sync_playwright() as p:
    browser = p.chromium.launch(channel="chrome",args=["--use-angle=swiftshader","--enable-unsafe-swiftshader"])
    page = browser.new_page(viewport={"width":1914,"height":909})
    page.goto("http://127.0.0.1:8765/?qa=1")
    page.wait_for_function("window.blitzTelemetry?.State === 'Home'",timeout=90000)
    for width,height in sizes:
        page.set_viewport_size({"width":width,"height":height})
        page.wait_for_timeout(300)
        home = wait_labels(page,["Play", "Settings", "How to Play", "WALLET", "STEP RIGHT UP"])
        if (width,height) in [(1914,909),(360,800),(800,360)]:
            page.screenshot(path=str(out/f"text-home-{width}x{height}.png"))
        action(page,1)
        settings = wait_labels(page,["Music", "Sound", "Reduced effects", "Home"])
        footer = next(label for label in settings if label["Text"] == "Home")
        assert footer["Bottom"] > .03, footer
        if (width,height) in [(1914,909),(360,800),(800,360)]:
            page.screenshot(path=str(out/f"text-settings-{width}x{height}.png"))
        home_row = 4 if page.evaluate("typeof navigator.vibrate === 'function'") else 3
        action(page,home_row)
        wait_labels(page,["How to Play"])
        report.append({"size":[width,height],"home":home,"settings":settings,"passed":True})
        print(f"PASS: Home and Settings render every label at {width}x{height}",flush=True)
    browser.close()
(out/"TextLayout.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
