"""Genera UI/perks.html a partir de perks.template.html incrustando el icono de Speed Cola (base64).
Uso:  python UI/build_ui.py      (edita siempre perks.template.html, no perks.html)
"""
import base64, pathlib

ui = pathlib.Path(__file__).parent
icon = ui.parent / "Client" / "assets" / "speedcola_icon.png"
b64 = base64.b64encode(icon.read_bytes()).decode()
html = (ui / "perks.template.html").read_text(encoding="utf-8").replace("__ICON__", "data:image/png;base64," + b64)
(ui / "perks.html").write_text(html, encoding="utf-8")
print("perks.html generado:", len(html), "bytes")
