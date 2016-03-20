#r "../packages/Octokit.0.19.0/lib/net45/Octokit.dll"
#r "../packages/FSharp.Configuration.0.5.12/lib/net40/FSharp.Configuration.dll"
#r "System.Configuration.dll"
#r "System.Core.dll"
#r "System.dll"

open System;
open System.Collections.Generic
open System.Threading.Tasks
open FSharp.Configuration
open Octokit

type CommitInfo = {Hash : string; Author : string; TimeStamp : DateTime; Message : string}
type CommittedFile = {LinesAdded: int option; LinesDeleted: int option; FileName: string}
type Commit = {CommitInfo: CommitInfo; Files: CommittedFile[]}

type Settings = AppSettings<"app.config">


let getCommit (client : GitHubClient) user repo ref = 
    async { 
        return! 
            client.Repository.Commit.Get(user, repo, ref) 
            |> Async.AwaitTask 
    }

let getCommits (client : GitHubClient) user repo = 
    async { 
        let! 
            commits = 
            client.Repository.Commit.GetAll(user, repo)
            |> Async.AwaitTask

        return! 
            commits
            |> Seq.map (fun c -> getCommit client c.Author.Login repo c.Sha)
            |> Async.Parallel
    }

let extractFileInfo (file : GitHubCommitFile) = 
     {
        LinesAdded = Some(file.Additions); 
        LinesDeleted = Some(file.Deletions); 
        FileName = file.Filename
     }

let extractCommitInfo (commit : GitHubCommit) =
    {
        CommitInfo = {Hash = commit.Commit.Sha.ToString(); Author = commit.Commit.Author.Name; TimeStamp = commit.Commit.Committer.Date.DateTime; Message = commit.Commit.Message}; 
        Files = 
            commit.Files
            |> Seq.toArray
            |> Array.map extractFileInfo
    }



let gitHubClient = new GitHubClient( new ProductHeaderValue(Settings.RepositoryHeader), Settings.RepositoryUri);
gitHubClient.Credentials = Credentials(Settings.RepositoryToken);

let commits = 
    getCommits gitHubClient Settings.RepositoryOwner Settings.RepositoryHeader
    |> Async.RunSynchronously

let totalCommits = 
    commits
    |> Array.map extractCommitInfo;

let numberOfCommits = 
    totalCommits 
    |> Array.length

let numberOfEntitiesChanged =
    totalCommits
    |> Array.collect(fun c -> c.Files)
    |> Array.length

let numberOfEntities = 
    totalCommits
    |> Array.collect(fun c -> c.Files)
    |> Array.groupBy(fun f -> f.FileName)
    |> Array.length
    
let numberOfAuthors =
    totalCommits
    |> Array.groupBy(fun c -> c.CommitInfo.Author)
    |> Array.distinct
    |> Array.length

let numberOfRevisions =
    totalCommits
    |> Array.collect(fun c -> c.Files)
    |> Array.groupBy(fun f -> f.FileName)
    |> Array.map ( fun c -> fst c, (snd c).Length)
    |> Array.sortByDescending ( fun c -> snd c)
    
