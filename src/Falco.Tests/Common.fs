﻿[<AutoOpen>]
module Falco.Tests.Common

open System
open System.IO
open System.IO.Pipelines
open System.Security.Claims
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open NSubstitute
open System.Collections.Generic

[<CLIMutable>]
type FakeRecord = { Name : string }

let getResponseBody (ctx : HttpContext) =
    task {
        ctx.Response.Body.Position <- 0L
        use reader = new StreamReader(ctx.Response.Body)
        return! reader.ReadToEndAsync()
    }

let getHttpContextWriteable (authenticated : bool) =
    let req = Substitute.For<HttpRequest>()
    req.Headers.Returns(Substitute.For<HeaderDictionary>()) |> ignore

    let resp = Substitute.For<HttpResponse>()
    let respBody = new MemoryStream()
    resp.Headers.Returns(Substitute.For<HeaderDictionary>()) |> ignore
    resp.BodyWriter.Returns(PipeWriter.Create(respBody)) |> ignore
    resp.Body <- respBody
    resp.StatusCode <- 200

    let services = Substitute.For<IServiceProvider>()

    let identity = Substitute.For<ClaimsIdentity>()
    identity.IsAuthenticated.Returns(authenticated) |> ignore

    let user = Substitute.For<ClaimsPrincipal>()
    user.Identity.Returns(identity) |> ignore

    let ctx = Substitute.For<HttpContext>()
    ctx.Request.Returns(req) |> ignore
    ctx.Response.Returns(resp) |> ignore
    ctx.RequestServices.Returns(services) |> ignore
    ctx.User.Returns(user) |> ignore

    ctx

let cookieCollection cookies =
  { new IRequestCookieCollection with
    member __.ContainsKey(key: string) = Map.containsKey key cookies
    member __.Count = Map.count cookies
    member __.GetEnumerator() = (Map.toSeq cookies |> Seq.map KeyValuePair).GetEnumerator()
    member __.GetEnumerator() = __.GetEnumerator() :> Collections.IEnumerator
    member __.Item with get (key: string): string = Map.find key cookies
    member __.Keys = Map.toSeq cookies |> Seq.map fst |> ResizeArray :> Collections.Generic.ICollection<string>
    member __.TryGetValue(key: string, value: byref<string>): bool =
      match Map.tryFind key cookies with
      | Some _ -> true
      | _ -> false }