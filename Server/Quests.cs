using System;
using System.Collections.Generic;
using System.IO;
using Path = System.IO.Path;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding.Custom;

namespace TarkovaCola.Server
{
    // ============================================================================================
    //  Misiones (via VisitAPI). El pack con las misiones vive en user/mods/VisitAPI-Server/packs/TarkovaCola/ (lo despliega el build).
    //  Este mod pone lo que el pack no puede:
    //   - el sobre de la mision de Skier (objeto de mision, clon de uno que ya existe),
    //   - el bloqueo de PHD Flopper en Therapist hasta completar la mision de Skier,
    //   - la ruta que le dice al cliente donde estan los guardias (para meterles las perks y el sobre al aparecer).
    //  Si VisitAPI no esta instalado, nada de esto se activa y PHD Flopper se vende como siempre.
    // ============================================================================================
    internal static class QuestIds
    {
        internal const string SkierQuest1 = "6a1c00000000000000000e01";
        internal const string Envelope = "6a1c00000000000000000f01";
        internal const string PackName = "TarkovaCola";
        internal const string InfoParent = "5448ecbe4bdc2d60728b4568";      // clase base de los objetos de informacion / mision

        internal static string PackDir
        {
            get
            {
                var mods = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(QuestIds).Assembly.Location) ?? ".", ".."));
                return Path.Combine(mods, "VisitAPI-Server", "packs", PackName);
            }
        }

        internal static string SpawnsFile { get { return Path.Combine(PackDir, "spawns", "TarkovaCola_Skier1.json"); } }
        internal static bool PackInstalled { get { return File.Exists(Path.Combine(PackDir, "pack.json")); } }

        // El punto de los guardias, tal como esta en el archivo de apariciones (null si falta o no esta listo)
        internal static string ReadPost()
        {
            try
            {
                if (!File.Exists(SpawnsFile)) return null;
                using var doc = JsonDocument.Parse(File.ReadAllText(SpawnsFile), new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
                foreach (var e in doc.RootElement.EnumerateArray()) return e.GetRawText();
            }
            catch (Exception ex) { Console.WriteLine("[Tarkova-Cola] No se pudo leer el punto de la mision: " + ex.Message); }
            return null;
        }

        // El punto esta colocado de verdad (el archivo trae "ready": true cuando alguien ha puesto las coordenadas)
        internal static bool PostReady()
        {
            var raw = ReadPost();
            if (raw == null) return false;
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.TryGetProperty("ready", out var r) && r.ValueKind == JsonValueKind.True;
        }
    }

    [Injectable(TypePriority = OnLoadOrder.Preload + 10)]
    public class TarkovaColaQuests : IOnLoad
    {
        private readonly CustomItemService _items;
        private readonly TemplateTable _templates;
        private readonly TradersTable _traders;

        public TarkovaColaQuests(CustomItemService items, TemplateTable templates, TradersTable traders)
        {
            _items = items;
            _templates = templates;
            _traders = traders;
        }

        public Task OnLoadAsync(CancellationToken cancellationToken)
        {
            try { CreateEnvelope(); }
            catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR creando el sobre: " + e); }
            try { LockPhd(); }
            catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR bloqueando PHD Flopper: " + e); }
            return Task.CompletedTask;
        }

        // El sobre: clon de un objeto de informacion de 1x1 (preferiblemente ya marcado como objeto de mision)
        private void CreateEnvelope()
        {
            var info = _templates.Items.Values
                .Where(t => t.Parent == new MongoId(QuestIds.InfoParent) && t.Properties != null && t.Properties.Width == 1 && t.Properties.Height == 1)
                .OrderByDescending(t => t.Properties.QuestItem == true)
                .FirstOrDefault();
            if (info == null) { Console.WriteLine("[Tarkova-Cola] No hay ningun objeto de informacion que clonar para el sobre"); return; }
            _items.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = info.Id,
                ParentId = info.Parent,
                NewId = new MongoId(QuestIds.Envelope),
                NewItemName = "tarkovacola_envelope",
                OverrideProperties = new TemplateItemProperties { QuestItem = true, ExaminedByDefault = true, CanSellOnRagfair = false },
                FleaPriceRoubles = 1,
                HandbookPriceRoubles = 1,
                HandbookParentId = "5b47574386f77428ca22b2ef",     // handbook: Info
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new LocaleDetails { Name = "Sealed envelope", ShortName = "Envelope", Description = "A sealed envelope with no sender. It smells faintly of cola." },
                    ["es"] = new LocaleDetails { Name = "Sobre sellado", ShortName = "Sobre", Description = "Un sobre sellado y sin remitente. Huele un poco a cola." },
                },
            });
            Console.WriteLine("[Tarkova-Cola] Sobre de la mision creado (clon de " + info.Name + ")");
        }

        // PHD Flopper solo se vende en Therapist cuando se completa la mision de Skier (si el pack de misiones esta instalado y listo)
        private void LockPhd()
        {
            if (!QuestIds.PackInstalled || !QuestIds.PostReady())
            {
                Console.WriteLine("[Tarkova-Cola] Mision de Skier no activa (falta VisitAPI o el punto no esta colocado): PHD Flopper se vende sin bloqueo");
                return;
            }
            var qa = _traders[Traders.THERAPIST].QuestAssort;
            if (qa == null) return;
            if (!qa.TryGetValue("success", out var success) || success == null) qa["success"] = success = new Dictionary<MongoId, MongoId>();
            success[new MongoId(Perks.PhdFlopper.TraderItemId)] = new MongoId(QuestIds.SkierQuest1);
            Console.WriteLine("[Tarkova-Cola] PHD Flopper bloqueado en Therapist hasta completar la mision de Skier");
        }
    }
}
