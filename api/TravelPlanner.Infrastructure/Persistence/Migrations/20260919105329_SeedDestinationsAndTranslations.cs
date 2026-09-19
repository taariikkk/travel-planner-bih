using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TravelPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedDestinationsAndTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DestinationTranslation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    BestTimeToVisit = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestinationTranslation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestinationTranslation_Destination_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destination",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Destination",
                columns: new[] { "Id", "BestTimeToVisit", "Description", "Name", "Region", "Tags" },
                values: new object[,]
                {
                    { new Guid("1a0a0001-0000-4000-8000-000000000001"), "april-oktobar", "Grad koji je Isa-beg Isaković osnovao 1462. godine oko trgovačke čaršije, danas poznate kao Baščaršija — lavirint kaldrmisanih uličica, Sebilj fontane i Vijećnice čija je obnovljena kupola simbol grada. Sarajevo nosi tragove i osmanskog, i austrougarskog, i socijalističkog nasljeđa: od Latinske ćuprije gdje je počeo Prvi svjetski rat, preko olimpijskog duha 1984. godine, do Tunela spasa koji je grad održao u životu tokom opsade. Uz sve to, čaršijska kafa, ćevapi i burek čine Sarajevo gradom koji se jednako doživljava svim čulima.", "Sarajevo", "Centralna Bosna", new List<string> { "historija", "kultura", "grad", "hrana" } },
                    { new Guid("1a0a0001-0000-4000-8000-000000000002"), "maj-septembar", "Simbol grada je Stari most — remek-djelo osmanskog graditelja Mimara Hajrudina iz 1566. godine, srušeno u ratu 1993. i vjerno obnovljeno 2004, danas pod zaštitom UNESCO-a. Mladići koji skaču sa 24 metra visokog mosta u hladnu Neretvu nastavljaju tradiciju staru nekoliko stoljeća. Stara čaršija oko mosta, sa zanatskim radnjama i ćevabdžinicama, čuva duh grada koji je oduvijek bio raskršće kultura.", "Mostar", "Hercegovina", new List<string> { "historija", "kultura", "rijeka", "hrana" } },
                    { new Guid("1a0a0001-0000-4000-8000-000000000003"), "maj-oktobar", "Poznat kao \"grad sunca i platana\", Trebinje čuva Kastel — staru čaršiju iz 18. stoljeća — te Arslanagića most iz 16. stoljeća, zadužbinu velikog vezira Mehmed-paše Sokolovića. Iznad grada bdije manastir Hercegovačka Gračanica, a u obližnjem manastiru Tvrdoš i drugim podrumima uzgaja se žilavka, autohtona hercegovačka sorta grožđa poznata još od 14. stoljeća. Planina Leotar iznad grada nudi staze za planinarenje i pogled na cijelu dolinu Trebišnjice.", "Trebinje", "Hercegovina", new List<string> { "vino", "kultura", "rijeka", "grad" } },
                    { new Guid("1a0a0001-0000-4000-8000-000000000004"), "jun-septembar", "Jedini bh. izlaz na Jadransko more — svega oko 24 kilometra obale — čini Neum omiljenom destinacijom za porodični ljetni odmor. Hotelski resorti, plaže i restorani sa svježim morskim specijalitetima privlače posjetioce iz cijele regije, dok blizina Dubrovnika i Hercegovine čini Neum praktičnom bazom za dnevne izlete.", "Neum", "Hercegovina", new List<string> { "more", "porodica", "wellness" } },
                    { new Guid("1a0a0001-0000-4000-8000-000000000005"), "decembar-mart (zima), jun-septembar (planinarenje)", "Jahorina je bila domaćin ženskih alpskih disciplina na Zimskim olimpijskim igrama 1984. godine i od tada je najpoznatije skijalište u BiH, sa vrhom Ogorjelica na oko 1916 metara nadmorske visine. Van sezone skijanja, planina nudi staze za planinarenje i biciklizam okružene bukovom šumom, uz čist planinski vazduh svega pola sata vožnje od Sarajeva.", "Jahorina", "Istočno Sarajevo", new List<string> { "planina", "zima", "avantura" } },
                    { new Guid("1a0a0001-0000-4000-8000-000000000006"), "decembar-mart (zima), jun-septembar (planinarenje)", "Domaćin muških alpskih disciplina na Olimpijadi 1984, Bjelašnica danas spaja moderne skijaške staze sa autentičnim planinskim selom Lukomirom — najvišim i najizolovanijim naseljenim mjestom u BiH, gdje se i dalje čuvaju stari stećci i pastirski način života. Ljeti je planina omiljena među planinarima i biciklistima zbog netaknute prirode i pogleda na okolne vrhove.", "Bjelašnica", "Centralna Bosna", new List<string> { "planina", "zima", "avantura", "priroda" } },
                    { new Guid("1a0a0001-0000-4000-8000-000000000007"), "april-oktobar", "Najveći grad Republike Srpske raste oko tvrđave Kastel na obali Vrbasa, čiji temelji sežu u rimsko i preosmansko doba, a današnji izgled poprima za vrijeme Ferhad-paše Sokolovića. Obnovljena Ferhadija džamija iz 16. stoljeća i katedrala Hrista Spasitelja svjedoče o vjerskoj raznolikosti grada, dok brze vode Vrbasa privlače kajakaše, raftere, pa i tradicionalne \"dajak\" čamce kojima se lokalci provlače kroz brzake. Kafanska kultura duž šetališta i brojni festivali daju gradu opušten, mladalački ritam.", "Banja Luka", "Republika Srpska", new List<string> { "grad", "kultura", "rijeka" } },
                    { new Guid("1a0a0001-0000-4000-8000-000000000008"), "maj-oktobar", "Travnik je gotovo 150 godina bio sjedište bosanskih vezira i prijestonica Osmanskog carstva u Bosni, s tvrđavom koja i danas dominira gradom. Rodno mjesto je nobelovca Ive Andrića, koji je grad ovjekovječio u romanu \"Travnička hronika\". Izvor Plava voda i okolna brda daju gradu poseban ambijent, a travnički sir, spravljen po staroj recepturi, jedan je od najpoznatijih bh. sireva.", "Travnik", "Centralna Bosna", new List<string> { "historija", "kultura", "planina" } },
                    { new Guid("1a0a0001-0000-4000-8000-000000000009"), "april-oktobar", "Počitelj je izuzetno dobro očuvano srednjovjekovno i osmansko naselje na strmoj obali Neretve, sa kamenom kulom, Hadži-Alijinom džamijom iz 16. stoljeća i uskim stepeništima koja se penju kroz staro jezgro. Od 1960-ih grad je poznat i kao umjetnička kolonija, koja mu je donijela dodatni sloj kulturnog života uz srednjovjekovnu arhitekturu.", "Počitelj", "Hercegovina", new List<string> { "historija", "kultura", "rijeka" } },
                    { new Guid("1a0a0001-0000-4000-8000-000000000010"), "maj-oktobar", "Višegrad je nosilac Mehmed-paša Sokolović mosta iz 1577. godine, remek-djela osmanske arhitekture pod zaštitom UNESCO-a, koji je Ivo Andrić ovjekovječio u romanu \"Na Drini ćuprija\" za koji je dobio Nobelovu nagradu. U blizini se nalazi i Andrićgrad, kamena gradska četvrt posvećena piščevom djelu, koju je podigao reditelj Emir Kusturica.", "Višegrad", "Istočna Bosna", new List<string> { "historija", "kultura", "rijeka" } },
                    { new Guid("1a0a0001-0000-4000-8000-000000000011"), "maj-oktobar", "Jajce je jedini grad na svijetu sa vodopadom usred samog centra — Pliva se sa oko 20 metara visine uliva u Vrbas, tik ispod srednjovjekovne tvrđave koja je nekad bila posljednje uporište nezavisnog Bosanskog kraljevstva. U gradu je 1943. održano drugo zasjedanje AVNOJ-a, kojim je postavljen temelj socijalističke Jugoslavije. Nekoliko kilometara uzvodno, stara vodenice na Plivskim jezerima (\"mlinčići\") dodatno svjedoče o dugoj istoriji ovog malog, ali slojevitog grada.", "Jajce", "Centralna Bosna", new List<string> { "historija", "priroda", "rijeka" } }
                });

            migrationBuilder.InsertData(
                table: "DestinationTranslation",
                columns: new[] { "Id", "BestTimeToVisit", "Description", "DestinationId", "LanguageCode" },
                values: new object[,]
                {
                    { new Guid("2a0a0001-0000-4000-8000-000000000001"), "april-oktobar", "Grad koji je Isa-beg Isaković osnovao 1462. godine oko trgovačke čaršije, danas poznate kao Baščaršija — lavirint kaldrmisanih uličica, Sebilj fontane i Vijećnice čija je obnovljena kupola simbol grada. Sarajevo nosi tragove i osmanskog, i austrougarskog, i socijalističkog nasljeđa: od Latinske ćuprije gdje je počeo Prvi svjetski rat, preko olimpijskog duha 1984. godine, do Tunela spasa koji je grad održao u životu tokom opsade. Uz sve to, čaršijska kafa, ćevapi i burek čine Sarajevo gradom koji se jednako doživljava svim čulima.", new Guid("1a0a0001-0000-4000-8000-000000000001"), "bs" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000002"), "April-October", "Founded in 1462 by Isa-beg Isaković around a trading bazaar now known as Baščaršija — a maze of cobbled lanes, the Sebilj fountain, and the City Hall (Vijećnica) whose rebuilt dome has become a city symbol. Sarajevo carries layers of Ottoman, Austro-Hungarian, and socialist heritage: from the Latin Bridge where World War I began, through the Olympic spirit of 1984, to the Tunnel of Hope that kept the city alive during the siege. Strong coffee, ćevapi, and burek round out a city best experienced with every sense.", new Guid("1a0a0001-0000-4000-8000-000000000001"), "en" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000003"), "maj-septembar", "Simbol grada je Stari most — remek-djelo osmanskog graditelja Mimara Hajrudina iz 1566. godine, srušeno u ratu 1993. i vjerno obnovljeno 2004, danas pod zaštitom UNESCO-a. Mladići koji skaču sa 24 metra visokog mosta u hladnu Neretvu nastavljaju tradiciju staru nekoliko stoljeća. Stara čaršija oko mosta, sa zanatskim radnjama i ćevabdžinicama, čuva duh grada koji je oduvijek bio raskršće kultura.", new Guid("1a0a0001-0000-4000-8000-000000000002"), "bs" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000004"), "May-September", "The city's symbol is Stari Most — a masterpiece built in 1566 by Ottoman architect Mimar Hajruddin, destroyed during the 1993 war and faithfully rebuilt in 2004, now a UNESCO World Heritage Site. Young divers leaping 24 meters into the cold Neretva below continue a tradition centuries old. The old bazaar surrounding the bridge, with its craft shops and grill restaurants, preserves the spirit of a city that has always stood at a crossroads of cultures.", new Guid("1a0a0001-0000-4000-8000-000000000002"), "en" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000005"), "maj-oktobar", "Poznat kao \"grad sunca i platana\", Trebinje čuva Kastel — staru čaršiju iz 18. stoljeća — te Arslanagića most iz 16. stoljeća, zadužbinu velikog vezira Mehmed-paše Sokolovića. Iznad grada bdije manastir Hercegovačka Gračanica, a u obližnjem manastiru Tvrdoš i drugim podrumima uzgaja se žilavka, autohtona hercegovačka sorta grožđa poznata još od 14. stoljeća. Planina Leotar iznad grada nudi staze za planinarenje i pogled na cijelu dolinu Trebišnjice.", new Guid("1a0a0001-0000-4000-8000-000000000003"), "bs" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000006"), "May-October", "Known as \"the city of sun and plane trees,\" Trebinje preserves Kastel — its 18th-century old town — and the 16th-century Arslanagić Bridge, endowed by the grand vizier Mehmed-pasha Sokolović. The Hercegovačka Gračanica monastery watches over the city from above, while the nearby Tvrdoš monastery and local cellars produce žilavka, an indigenous Herzegovinian grape variety cultivated since the 14th century. Mount Leotar above town offers hiking trails and sweeping views of the Trebišnjica valley.", new Guid("1a0a0001-0000-4000-8000-000000000003"), "en" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000007"), "jun-septembar", "Jedini bh. izlaz na Jadransko more — svega oko 24 kilometra obale — čini Neum omiljenom destinacijom za porodični ljetni odmor. Hotelski resorti, plaže i restorani sa svježim morskim specijalitetima privlače posjetioce iz cijele regije, dok blizina Dubrovnika i Hercegovine čini Neum praktičnom bazom za dnevne izlete.", new Guid("1a0a0001-0000-4000-8000-000000000004"), "bs" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000008"), "June-September", "Bosnia's only stretch of Adriatic coastline — roughly 24 kilometers — makes Neum a favorite for family summer holidays. Hotel resorts, beaches, and restaurants serving fresh seafood draw visitors from across the region, while its proximity to Dubrovnik and Herzegovina makes it a convenient base for day trips.", new Guid("1a0a0001-0000-4000-8000-000000000004"), "en" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000009"), "decembar-mart (zima), jun-septembar (planinarenje)", "Jahorina je bila domaćin ženskih alpskih disciplina na Zimskim olimpijskim igrama 1984. godine i od tada je najpoznatije skijalište u BiH, sa vrhom Ogorjelica na oko 1916 metara nadmorske visine. Van sezone skijanja, planina nudi staze za planinarenje i biciklizam okružene bukovom šumom, uz čist planinski vazduh svega pola sata vožnje od Sarajeva.", new Guid("1a0a0001-0000-4000-8000-000000000005"), "bs" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000010"), "December-March (skiing), June-September (hiking)", "Jahorina hosted the women's alpine skiing events at the 1984 Winter Olympics and remains Bosnia's best-known ski resort, with its Ogorjelica peak reaching around 1,916 meters. Outside ski season, the mountain offers hiking and biking trails through beech forest, with clean mountain air just half an hour from Sarajevo.", new Guid("1a0a0001-0000-4000-8000-000000000005"), "en" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000011"), "decembar-mart (zima), jun-septembar (planinarenje)", "Domaćin muških alpskih disciplina na Olimpijadi 1984, Bjelašnica danas spaja moderne skijaške staze sa autentičnim planinskim selom Lukomirom — najvišim i najizolovanijim naseljenim mjestom u BiH, gdje se i dalje čuvaju stari stećci i pastirski način života. Ljeti je planina omiljena među planinarima i biciklistima zbog netaknute prirode i pogleda na okolne vrhove.", new Guid("1a0a0001-0000-4000-8000-000000000006"), "bs" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000012"), "December-March (skiing), June-September (hiking)", "Host of the men's alpine skiing events at the 1984 Olympics, Bjelašnica today pairs modern ski slopes with the authentic mountain village of Lukomir — the highest and most isolated inhabited settlement in Bosnia, where medieval stećci tombstones and a traditional shepherding life endure. In summer the mountain draws hikers and cyclists for its untouched landscape and panoramic views.", new Guid("1a0a0001-0000-4000-8000-000000000006"), "en" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000013"), "april-oktobar", "Najveći grad Republike Srpske raste oko tvrđave Kastel na obali Vrbasa, čiji temelji sežu u rimsko i preosmansko doba, a današnji izgled poprima za vrijeme Ferhad-paše Sokolovića. Obnovljena Ferhadija džamija iz 16. stoljeća i katedrala Hrista Spasitelja svjedoče o vjerskoj raznolikosti grada, dok brze vode Vrbasa privlače kajakaše, raftere, pa i tradicionalne \"dajak\" čamce kojima se lokalci provlače kroz brzake. Kafanska kultura duž šetališta i brojni festivali daju gradu opušten, mladalački ritam.", new Guid("1a0a0001-0000-4000-8000-000000000007"), "bs" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000014"), "April-October", "The largest city in Republika Srpska grew up around the Kastel fortress on the banks of the Vrbas, with roots reaching back to Roman and pre-Ottoman times and its present form shaped under Ferhad-pasha Sokolović. The rebuilt 16th-century Ferhadija Mosque and the Cathedral of Christ the Saviour reflect the city's religious diversity, while the fast-flowing Vrbas draws kayakers, rafters, and even traditional \"dajak\" pole-boats used by locals to navigate the rapids. A lively café culture along the riverside promenades and numerous festivals give the city a relaxed, youthful energy.", new Guid("1a0a0001-0000-4000-8000-000000000007"), "en" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000015"), "maj-oktobar", "Travnik je gotovo 150 godina bio sjedište bosanskih vezira i prijestonica Osmanskog carstva u Bosni, s tvrđavom koja i danas dominira gradom. Rodno mjesto je nobelovca Ive Andrića, koji je grad ovjekovječio u romanu \"Travnička hronika\". Izvor Plava voda i okolna brda daju gradu poseban ambijent, a travnički sir, spravljen po staroj recepturi, jedan je od najpoznatijih bh. sireva.", new Guid("1a0a0001-0000-4000-8000-000000000008"), "bs" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000016"), "May-October", "For nearly 150 years, Travnik was the seat of Bosnia's Ottoman viziers and the province's capital, with a hilltop fortress that still dominates the town. It's the birthplace of Nobel laureate Ivo Andrić, who immortalized the town in his novel \"Bosnian Chronicle.\" The Plava Voda spring and surrounding hills give the town its distinctive charm, and Travnik cheese, made to an old recipe, is one of Bosnia's best-known cheeses.", new Guid("1a0a0001-0000-4000-8000-000000000008"), "en" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000017"), "april-oktobar", "Počitelj je izuzetno dobro očuvano srednjovjekovno i osmansko naselje na strmoj obali Neretve, sa kamenom kulom, Hadži-Alijinom džamijom iz 16. stoljeća i uskim stepeništima koja se penju kroz staro jezgro. Od 1960-ih grad je poznat i kao umjetnička kolonija, koja mu je donijela dodatni sloj kulturnog života uz srednjovjekovnu arhitekturu.", new Guid("1a0a0001-0000-4000-8000-000000000009"), "bs" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000018"), "April-October", "Počitelj is an exceptionally well-preserved medieval and Ottoman-era settlement perched above the Neretva River, with a stone tower, the 16th-century Hadži-Alija Mosque, and narrow stepped lanes climbing through the old core. Since the 1960s the town has also been known as an artists' colony, adding a layer of living culture to its medieval architecture.", new Guid("1a0a0001-0000-4000-8000-000000000009"), "en" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000019"), "maj-oktobar", "Višegrad je nosilac Mehmed-paša Sokolović mosta iz 1577. godine, remek-djela osmanske arhitekture pod zaštitom UNESCO-a, koji je Ivo Andrić ovjekovječio u romanu \"Na Drini ćuprija\" za koji je dobio Nobelovu nagradu. U blizini se nalazi i Andrićgrad, kamena gradska četvrt posvećena piščevom djelu, koju je podigao reditelj Emir Kusturica.", new Guid("1a0a0001-0000-4000-8000-000000000010"), "bs" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000020"), "May-October", "Višegrad is home to the 1577 Mehmed Paša Sokolović Bridge, a masterpiece of Ottoman architecture and a UNESCO World Heritage Site, immortalized by Ivo Andrić in his Nobel Prize-winning novel \"The Bridge on the Drina.\" Nearby stands Andrićgrad, a stone town quarter built by director Emir Kusturica in tribute to the writer's work.", new Guid("1a0a0001-0000-4000-8000-000000000010"), "en" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000021"), "maj-oktobar", "Jajce je jedini grad na svijetu sa vodopadom usred samog centra — Pliva se sa oko 20 metara visine uliva u Vrbas, tik ispod srednjovjekovne tvrđave koja je nekad bila posljednje uporište nezavisnog Bosanskog kraljevstva. U gradu je 1943. održano drugo zasjedanje AVNOJ-a, kojim je postavljen temelj socijalističke Jugoslavije. Nekoliko kilometara uzvodno, stara vodenice na Plivskim jezerima (\"mlinčići\") dodatno svjedoče o dugoj istoriji ovog malog, ali slojevitog grada.", new Guid("1a0a0001-0000-4000-8000-000000000011"), "bs" },
                    { new Guid("2a0a0001-0000-4000-8000-000000000022"), "May-October", "Jajce is the only town in the world with a waterfall right at its center — the Pliva plunges roughly 20 meters into the Vrbas, just below the medieval fortress that was once the last stronghold of the independent Bosnian Kingdom. In 1943 the town hosted the second AVNOJ session, which laid the foundation for socialist Yugoslavia. A few kilometers upstream, old watermills on the Pliva Lakes (\"mlinčići\") add another layer to this small but historically rich town.", new Guid("1a0a0001-0000-4000-8000-000000000011"), "en" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DestinationTranslation_DestinationId_LanguageCode",
                table: "DestinationTranslation",
                columns: new[] { "DestinationId", "LanguageCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DestinationTranslation");

            migrationBuilder.DeleteData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000004"));

            migrationBuilder.DeleteData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000005"));

            migrationBuilder.DeleteData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000006"));

            migrationBuilder.DeleteData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000007"));

            migrationBuilder.DeleteData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000008"));

            migrationBuilder.DeleteData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000009"));

            migrationBuilder.DeleteData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000010"));

            migrationBuilder.DeleteData(
                table: "Destination",
                keyColumn: "Id",
                keyValue: new Guid("1a0a0001-0000-4000-8000-000000000011"));
        }
    }
}
