<?php

include("config.php");

$key = $_POST["key"];
$pseudo = $_POST["playerName"];
$mdp = md5($_POST["password"]);

$query = "SELECT * FROM VerifKey WHERE keyGame='$key'";
$rowKey = $conn->query($query);

$UserExist = $conn->query("SELECT * FROM users WHERE username='$pseudo' AND password='$mdp'");

if($rowKey->num_rows == 1){
	if($UserExist->num_rows == 1){
		
		echo "Login success";
		
	}else{
		echo "Error login";
	}
}else{
	echo "Error key";
}

?>