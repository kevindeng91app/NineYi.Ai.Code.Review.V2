
## Reference


https://support.atlassian.com/bitbucket-cloud/docs/event-payloads/#Pull-request-events


## nodes

- $.pullrequest.destination.repository.name = AllenLin.NineYi.WebStore.MobileWebMall
- $.pullrequest.destination.branch.name = develop
- $.pullrequest.links.diff.href = https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall/diff/nineyi/allenlin.nineyi.webstore.mobilewebmall:6634f9edfbf6%0D5b928548c998?from_pullrequest_id=1&topic=true
- $.pullrequest.links.comments.href = https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall/pullrequests/1/comments
- $.actor.account_id = 712020:31f021b5-b1ac-4c63-abcb-d9d121b92d18
- $.pullrequest.type = pullrequest
- $.pullrequest.destination.commit.hash = 5b928548c998
- $.pullrequest.links.commits.href = https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall/pullrequests/1/commits
- $.pullrequest.title = Feature/VSTS572807 MY 門市自取帶Postcode fix Atomete
- $.pullrequest.links.diffstat.href = https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall/diffstat/nineyi/allenlin.nineyi.webstore.mobilewebmall:6634f9edfbf6%0D5b928548c998?from_pullrequest_id=1&topic=true


## sample


```json
{
  "repository": {
    "type": "repository",
    "full_name": "nineyi/allenlin.nineyi.webstore.mobilewebmall",
    "links": {
      "self": {
        "href": "https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall"
      },
      "html": {
        "href": "https://bitbucket.org/nineyi/allenlin.nineyi.webstore.mobilewebmall"
      },
      "avatar": {
        "href": "https://bytebucket.org/ravatar/%7Bb0b4a8de-3d39-441b-88f1-e4e9bca4bb51%7D?ts=c_sharp"
      }
    },
    "name": "AllenLin.NineYi.WebStore.MobileWebMall",
    "scm": "git",
    "website": null,
    "owner": {
      "display_name": "91App, Inc.",
      "links": {
        "self": {
          "href": "https://api.bitbucket.org/2.0/workspaces/%7Bea473a3d-1a27-4577-9cd7-b4d80d73187a%7D"
        },
        "avatar": {
          "href": "https://bitbucket.org/account/nineyi/avatar/"
        },
        "html": {
          "href": "https://bitbucket.org/%7Bea473a3d-1a27-4577-9cd7-b4d80d73187a%7D/"
        }
      },
      "type": "team",
      "uuid": "{ea473a3d-1a27-4577-9cd7-b4d80d73187a}",
      "username": "nineyi"
    },
    "workspace": {
      "type": "workspace",
      "uuid": "{ea473a3d-1a27-4577-9cd7-b4d80d73187a}",
      "name": "91App, Inc.",
      "slug": "nineyi",
      "links": {
        "avatar": {
          "href": "https://bitbucket.org/workspaces/nineyi/avatar/?ts=1679885898"
        },
        "html": {
          "href": "https://bitbucket.org/nineyi/"
        },
        "self": {
          "href": "https://api.bitbucket.org/2.0/workspaces/nineyi"
        }
      }
    },
    "is_private": true,
    "project": {
      "type": "project",
      "key": "ALLLIN",
      "uuid": "{921cad82-1390-410a-9070-dfeddc9354ba}",
      "name": "AllenLin",
      "links": {
        "self": {
          "href": "https://api.bitbucket.org/2.0/workspaces/nineyi/projects/ALLLIN"
        },
        "html": {
          "href": "https://bitbucket.org/nineyi/workspace/projects/ALLLIN"
        },
        "avatar": {
          "href": "https://bitbucket.org/nineyi/workspace/projects/ALLLIN/avatar/32?ts=1749458253"
        }
      }
    },
    "uuid": "{b0b4a8de-3d39-441b-88f1-e4e9bca4bb51}",
    "parent": {
      "type": "repository",
      "full_name": "nineyi/nineyi.webstore.mobilewebmall",
      "links": {
        "self": {
          "href": "https://api.bitbucket.org/2.0/repositories/nineyi/nineyi.webstore.mobilewebmall"
        },
        "html": {
          "href": "https://bitbucket.org/nineyi/nineyi.webstore.mobilewebmall"
        },
        "avatar": {
          "href": "https://bytebucket.org/ravatar/%7Bbefde296-dd32-49b7-b1ca-18932b69fefc%7D?ts=c_sharp"
        }
      },
      "name": "NineYi.WebStore.MobileWebMall",
      "uuid": "{befde296-dd32-49b7-b1ca-18932b69fefc}"
    }
  },
  "actor": {
    "display_name": "Allen Lin",
    "links": {
      "self": {
        "href": "https://api.bitbucket.org/2.0/users/%7B03eb6e93-17ce-478f-84f7-6323c38557ec%7D"
      },
      "avatar": {
        "href": "https://avatar-management--avatars.us-west-2.prod.public.atl-paas.net/712020:31f021b5-b1ac-4c63-abcb-d9d121b92d18/3d90f163-50e3-45de-bedc-b4c47da369ba/128"
      },
      "html": {
        "href": "https://bitbucket.org/%7B03eb6e93-17ce-478f-84f7-6323c38557ec%7D/"
      }
    },
    "type": "user",
    "uuid": "{03eb6e93-17ce-478f-84f7-6323c38557ec}",
    "account_id": "712020:31f021b5-b1ac-4c63-abcb-d9d121b92d18",
    "nickname": "Allen Lin"
  },
  "pullrequest": {
    "comment_count": 0,
    "task_count": 0,
    "type": "pullrequest",
    "id": 1,
    "title": "Feature/VSTS572807 MY 門市自取帶Postcode fix Atomete",
    "description": "",
    "rendered": {
      "title": {
        "type": "rendered",
        "raw": "Feature/VSTS572807 MY 門市自取帶Postcode fix Atomete",
        "markup": "markdown",
        "html": "<p>Feature/<a href=\"https://91appinc.visualstudio.com/web/wi.aspx?id=572807\" class=\"ap-connect-link\" rel=\"nofollow\">VSTS572807</a> MY 門市自取帶Postcode fix Atomete</p>"
      },
      "description": {
        "type": "rendered",
        "raw": "",
        "markup": "markdown",
        "html": ""
      }
    },
    "state": "OPEN",
    "draft": false,
    "merge_commit": null,
    "close_source_branch": false,
    "closed_by": null,
    "author": {
      "display_name": "Allen Lin",
      "links": {
        "self": {
          "href": "https://api.bitbucket.org/2.0/users/%7B03eb6e93-17ce-478f-84f7-6323c38557ec%7D"
        },
        "avatar": {
          "href": "https://avatar-management--avatars.us-west-2.prod.public.atl-paas.net/712020:31f021b5-b1ac-4c63-abcb-d9d121b92d18/3d90f163-50e3-45de-bedc-b4c47da369ba/128"
        },
        "html": {
          "href": "https://bitbucket.org/%7B03eb6e93-17ce-478f-84f7-6323c38557ec%7D/"
        }
      },
      "type": "user",
      "uuid": "{03eb6e93-17ce-478f-84f7-6323c38557ec}",
      "account_id": "712020:31f021b5-b1ac-4c63-abcb-d9d121b92d18",
      "nickname": "Allen Lin"
    },
    "reason": "",
    "created_on": "2026-02-21T14:18:33.992854+00:00",
    "updated_on": "2026-02-21T14:18:34.538718+00:00",
    "destination": {
      "branch": {
        "name": "develop"
      },
      "commit": {
        "hash": "5b928548c998",
        "links": {
          "self": {
            "href": "https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall/commit/5b928548c998"
          },
          "html": {
            "href": "https://bitbucket.org/nineyi/allenlin.nineyi.webstore.mobilewebmall/commits/5b928548c998"
          }
        },
        "type": "commit"
      },
      "repository": {
        "type": "repository",
        "full_name": "nineyi/allenlin.nineyi.webstore.mobilewebmall",
        "links": {
          "self": {
            "href": "https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall"
          },
          "html": {
            "href": "https://bitbucket.org/nineyi/allenlin.nineyi.webstore.mobilewebmall"
          },
          "avatar": {
            "href": "https://bytebucket.org/ravatar/%7Bb0b4a8de-3d39-441b-88f1-e4e9bca4bb51%7D?ts=c_sharp"
          }
        },
        "name": "AllenLin.NineYi.WebStore.MobileWebMall",
        "uuid": "{b0b4a8de-3d39-441b-88f1-e4e9bca4bb51}"
      }
    },
    "source": {
      "branch": {
        "name": "feature/VSTS8959-QA300-HK-Global-Develop",
        "links": {},
        "sync_strategies": [
          "merge_commit",
          "rebase"
        ]
      },
      "commit": {
        "hash": "6634f9edfbf6",
        "links": {
          "self": {
            "href": "https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall/commit/6634f9edfbf6"
          },
          "html": {
            "href": "https://bitbucket.org/nineyi/allenlin.nineyi.webstore.mobilewebmall/commits/6634f9edfbf6"
          }
        },
        "type": "commit"
      },
      "repository": {
        "type": "repository",
        "full_name": "nineyi/allenlin.nineyi.webstore.mobilewebmall",
        "links": {
          "self": {
            "href": "https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall"
          },
          "html": {
            "href": "https://bitbucket.org/nineyi/allenlin.nineyi.webstore.mobilewebmall"
          },
          "avatar": {
            "href": "https://bytebucket.org/ravatar/%7Bb0b4a8de-3d39-441b-88f1-e4e9bca4bb51%7D?ts=c_sharp"
          }
        },
        "name": "AllenLin.NineYi.WebStore.MobileWebMall",
        "uuid": "{b0b4a8de-3d39-441b-88f1-e4e9bca4bb51}"
      }
    },
    "reviewers": [],
    "participants": [],
    "links": {
      "self": {
        "href": "https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall/pullrequests/1"
      },
      "html": {
        "href": "https://bitbucket.org/nineyi/allenlin.nineyi.webstore.mobilewebmall/pull-requests/1"
      },
      "commits": {
        "href": "https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall/pullrequests/1/commits"
      },
      "approve": {
        "href": "https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall/pullrequests/1/approve"
      },
      "request-changes": {
        "href": "https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall/pullrequests/1/request-changes"
      },
      "diff": {
        "href": "https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall/diff/nineyi/allenlin.nineyi.webstore.mobilewebmall:6634f9edfbf6%0D5b928548c998?from_pullrequest_id=1&topic=true"
      },
      "diffstat": {
        "href": "https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall/diffstat/nineyi/allenlin.nineyi.webstore.mobilewebmall:6634f9edfbf6%0D5b928548c998?from_pullrequest_id=1&topic=true"
      },
      "comments": {
        "href": "https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall/pullrequests/1/comments"
      },
      "activity": {
        "href": "https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall/pullrequests/1/activity"
      },
      "merge": {
        "href": "https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall/pullrequests/1/merge"
      },
      "decline": {
        "href": "https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall/pullrequests/1/decline"
      },
      "statuses": {
        "href": "https://api.bitbucket.org/2.0/repositories/nineyi/allenlin.nineyi.webstore.mobilewebmall/pullrequests/1/statuses"
      }
    },
    "summary": {
      "type": "rendered",
      "raw": "",
      "markup": "markdown",
      "html": ""
    }
  }
}
```