# API

## General

 1. POST `/api/{controller}/create`
  * Returns `{ success: true | false, uuid: '...' }`
 2. PUT `/api/{controller}/update`
  * Can take partial models and update them
  * Works like `{ ...newUpdates, ...existingModel }`  
  * Returns `{ success: true | false, uuid: '...' }`
 3. DELETE /api/{controller}/delete
  * Returns `{ success: true | false, uuid: '...' }`
 4. Exceptions : 500, Not found: 404, all endpoints can return 401 if not logged in, except `/api/user/login`

## User

 1. POST `/api/user/login`
  * Parameter: `password`
  * Return `{success: true|false}`
  * Note: this is just to get to initialize the user. A server needs to boot up as well
    which will happen when login information is available.

 2. GET /api/user
  * Gets all information about the user.
  * Returns `UserInfoModel`

 3. `/api/user/create` and `/api/user/update`
 4. GET /api/user/randomSeed
   * Generates a random seed
   * Return `<randomSeed>`

### Models

UserInfoModel: `{username: required, description: required, photoUrl, baseSignature: required, publicKey: required}`

## User Setting Controller

 1. GET `/api/userSetting`
  * Get user settings (UI settings, like dark/light mode, Tor mode, etc.)
  * Return `UserSettingModel`
 2. PUT `/api/userSetting/update`

## Product

 1. GET `/api/product/{id}`
    * Return a `MarketItemModel`
 2. POST `/api/product/create`
 2. GET `/api/product/bought/{publicKey?}`
    * Without any parameter: get the logged in users bought products
    * With publicKey: Lookup other bought items
    * List of `MarketItemModel`s
 3. GET `/api/product/sold/{publicKey?}`
    * Without any parameter: get the logged in users sold products
    * With publicKey: Lookup other sold items
    * List of `MarketItemModel`s
 4. PUT `/api/product/update`

### Models

MarketItemModel (all fields required): `
{
nameType,
title,
description,
shipping,
dealType,
category,
price: <float>,
priceType: <int>,
state: <int>,
photoUrls: <[string]>,
baseSignature,
buyerOnionEndpoint,
createdUtc: <date>,
signature,
hash,
fineness,
weightInGrams,
size,
manufacturer,
xrcReceivingAddress,
xrcTransactionhash
}
`

## Peer

 1. /api/peer/current

### Models

## Currency

 1. `/api/currency/{from}/{to}`
  * Get a price quote of supported currencies.
  * Support currencies: XRC, BTC, LTC
  * Returns `{ price }`

## Block

Technical details for the chains.

## Chat