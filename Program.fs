// For more information see https://aka.ms/fsharp-console-apps
open System.IO
open System.Text
open System.Text.Json
open FSharp.Data

System.Globalization.CultureInfo.DefaultThreadCurrentCulture <- System.Globalization.CultureInfo.InvariantCulture
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture <- System.Globalization.CultureInfo.InvariantCulture
[<Literal>]
let ss = @"https://raw.githubusercontent.com/aszabo314/passpunkte.json-script/master/DJI_0042_points.json"
type Pp = JsonProvider<ss>

let infolder = @"C:\bla\passpunkte\knauf_Q4_2021-ortho"
let system = "+proj=tmerc +lat_0=0 +lon_0=31 +k=1 +x_0=0 +y_0=-5000000 +ellps=bessel +pm=ferro +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs +type=crs"

let files = 
    Directory.GetFiles(infolder)
    |> Array.filter (fun p -> Path.GetExtension(p).ToLowerInvariant() = ".json")
    |> Array.choose (fun p ->let nn = Path.GetFileNameWithoutExtension(p) in if nn="passpunkte" then None else Some (nn, p))

let mutable points = Map.empty
let obses = 
    files |> Array.collect (fun (photoname,path) -> 
            let jsonobses = Pp.Load path
            jsonobses |> Array.choose (fun obs -> 
                let n = 
                    obs.Name.Split(" ") 
                    |> Array.map (fun s -> s.Trim())
                    |> Array.filter (fun s -> s <> "")
                let obsname = 
                    try
                        if n.Length = 1 then 
                            Some n.[0]
                        elif n.Length = 4 then 
                            let x = System.Double.Parse n.[1]
                            let y = System.Double.Parse n.[2]
                            let z = System.Double.Parse n.[3]
                            let name = n.[0]
                            points <- points |> Map.add name [|x;y;z|]
                            Some name
                        else None
                    with e -> 
                        System.Console.WriteLine("{0}",e.Message)
                        None
                obsname |> Option.map (fun obsname -> 
                    {|photo=photoname; point=obsname; pixel=[|obs.PxX; obs.PxY|]; ndc=[|obs.NdcX;obs.NdcY|]|}
                )
            )
        )

let out =
    {|
        system = system
        points = points 
        observations = obses
    |}

let opts = JsonSerializerOptions(WriteIndented = true)
opts.Encoder <- System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
let jsonstring = JsonSerializer.Serialize(out,opts)

let outpath = Path.Combine [|infolder;"passpunkte.json"|]
File.WriteAllText(outpath, jsonstring)
System.Console.WriteLine("written {0}",outpath)