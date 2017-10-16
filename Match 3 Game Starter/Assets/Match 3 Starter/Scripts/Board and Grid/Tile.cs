/*
 * Copyright (c) 2017 Razeware LLC
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tile : MonoBehaviour {
	private static Color selectedColor = new Color(.5f, .5f, .5f, 1.0f);
	private static Tile previousSelected = null;

	private SpriteRenderer render;
	private bool isSelected = false;
	Vector2 upleft = new Vector2 (-1, 1);
	Vector2 upright = new Vector2 (1, 1);
	Vector2 downleft = new Vector2 (-1, -1);
	Vector2 downright = new Vector2 (1, -1);
	private Vector2[] adjacentDirections = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

	private bool matchFound = false;
	private static bool matchOccured = false;
	private static bool isAnimating = false;

	void Awake() {
		render = GetComponent<SpriteRenderer>();
    }

	private void Select() {
		isSelected = true;
		render.color = selectedColor;
		previousSelected = gameObject.GetComponent<Tile>();
		SFXManager.instance.PlaySFX(Clip.Select);
	}

	private void Deselect() {
		isSelected = false;
		render.color = Color.white;
		previousSelected = null;
	}

	private void Explode() {
		List<GameObject> surroundingTiles = GetSurroundingTiles ();
		for (int i = 0; i < surroundingTiles.Count; i++) {
			if (surroundingTiles [i] != null) {
				surroundingTiles [i].GetComponent<SpriteRenderer> ().sprite = null;
			}
			render.sprite = null;
		}
		StopCoroutine (BoardManager.instance.FindNullTiles ());
		StartCoroutine (BoardManager.instance.FindNullTiles ());
		SFXManager.instance.PlaySFX (Clip.Explosion);
		GUIManager.instance.MoveCounter--;
	}

	void OnMouseDown() {
		if (render.sprite == null || BoardManager.instance.IsShifting || isAnimating == true) {
			return;
		}
		if (isSelected) { //Is it already selected?
			if (render.sprite.name == "bomb") {
				Explode ();
			}
			Deselect ();
			} else {
			if (previousSelected == null) { //Is it the first tile selected?
				Select ();
			} else {
					if (GetAllAdjacentTiles ().Contains (previousSelected.gameObject)) {
					StartCoroutine (AnimateSwapSprite ());
			} else {
				previousSelected.GetComponent<Tile> ().Deselect ();
				Select ();
						}
					}
				}
			}

	public void SwapSprite(SpriteRenderer render2) {
		if (render.sprite == render2.sprite) {
			return;
		}
		Sprite tempSprite = render2.sprite;
		render2.sprite = render.sprite;
		render.sprite = tempSprite;
		SFXManager.instance.PlaySFX (Clip.Swap);
		GUIManager.instance.MoveCounter--;
	}

	private IEnumerator AnimateSwapSprite(bool checkMatches = true)
	{
		isAnimating = true;
		Vector2 prevPos = previousSelected.transform.position;
		Vector2 currPos = transform.position;

		float timer = 0;
		float timeToMove = 0.2f;

		while (timer < timeToMove) 
		{
			previousSelected.transform.position = Vector2.Lerp (prevPos, currPos, timer / timeToMove);
			transform.position = Vector2.Lerp (currPos, prevPos, timer / timeToMove);

			yield return null;
			timer += Time.deltaTime;
		}
		previousSelected.transform.position = prevPos;
		transform.position = currPos;
		SwapSprite (previousSelected.render);

		if (checkMatches == true) {
			previousSelected.ClearAllMatches ();
			ClearAllMatches ();

			if (matchOccured == false) {
				StartCoroutine (AnimateSwapSprite (false));
				GUIManager.instance.MoveCounter = GUIManager.instance.MoveCounter + 2;
				SFXManager.instance.PlaySFX (Clip.Incorrect);
			} else {
				previousSelected.Deselect ();
				isAnimating = false;
			}
			matchOccured = false;
		} else {
			previousSelected.Deselect ();
			isAnimating = false;
		}
	}

	private GameObject GetAdjacent(Vector2 castDir) {
		RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir);
		if (hit.collider != null) {
			return hit.collider.gameObject;
		}
		return null;
	}
		
	private GameObject GetSurrounding(Vector2 castDir, Vector2 startPos) {
		RaycastHit2D hit = Physics2D.Raycast(startPos, castDir);
		if (hit.collider != null) {
			return hit.collider.gameObject;
		}
		return null;
	}

	private List<GameObject> GetAllAdjacentTiles() {
		List<GameObject> adjacentTiles = new List<GameObject> ();
		for (int i = 0; i < adjacentDirections.Length; i++) {
			adjacentTiles.Add (GetAdjacent (adjacentDirections [i]));
		}
		return adjacentTiles;
	}

	private List<GameObject> GetSurroundingTiles() {
		List<GameObject> surroundingTiles = new List<GameObject> ();
		GameObject topObject = GetAdjacent(Vector2.up);
		if (topObject != null) {
			surroundingTiles.Add (topObject);
			surroundingTiles.Add (GetSurrounding (Vector2.left, topObject.transform.position));
			surroundingTiles.Add (GetSurrounding (Vector2.right, topObject.transform.position));
		}
		GameObject bottomObject = GetAdjacent (Vector2.down);
		if (bottomObject != null) {
			surroundingTiles.Add (bottomObject);
			surroundingTiles.Add (GetSurrounding (Vector2.left, bottomObject.transform.position));
			surroundingTiles.Add (GetSurrounding (Vector2.right, bottomObject.transform.position));
		}
		surroundingTiles.Add (GetSurrounding (Vector2.left, transform.position));
		surroundingTiles.Add (GetSurrounding (Vector2.right, transform.position));
		return surroundingTiles;
	}

	private List<GameObject> FindMatch(Vector2 castDir) {
		List<GameObject> matchingTiles = new List<GameObject>();
		RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir);
		while (hit.collider != null && hit.collider.GetComponent<SpriteRenderer>().sprite == render.sprite) {
			matchingTiles.Add(hit.collider.gameObject);
			hit = Physics2D.Raycast(hit.collider.transform.position, castDir);
		}
		return matchingTiles;
	}

	private void ClearMatch(Vector2[] paths)
	{
		List<GameObject> matchingTiles = new List<GameObject>();
		for (int i = 0; i < paths.Length; i++)
		{
			matchingTiles.AddRange(FindMatch(paths[i]));
		}
		if (matchingTiles.Count >= 2)
		{
			for (int i = 0; i < matchingTiles.Count; i++)
			{
				matchingTiles[i].GetComponent<SpriteRenderer>().sprite = null; //<--- looky looky Here's the Cookie
			}
			matchFound = true;
		}
	}

	public void ClearAllMatches ()
	{
		if (render.sprite == null)
			return;

		ClearMatch (new Vector2[2] { Vector2.left, Vector2.right });
		ClearMatch (new Vector2[2] { Vector2.up, Vector2.down });
		if (matchFound == true) {
			if (render.sprite.name == "bomb") {
				BoardManager.instance.ClearAll ();
				SFXManager.instance.PlaySFX (Clip.Explosion);
				matchOccured = true;
				matchFound = false;
			} else {
				render.sprite = null;
				matchFound = false;
				matchOccured = true;

				StopCoroutine (BoardManager.instance.FindNullTiles ());
				StartCoroutine (BoardManager.instance.FindNullTiles ());

				SFXManager.instance.PlaySFX (Clip.Clear);
			}
		}
	}
}
