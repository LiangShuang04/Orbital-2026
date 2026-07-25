<?php

include("config.php");

$key = $_POST["key"];
$pseudo = $_POST["playerName"];
$mdp = md5($_POST["password"]);

$query = "SELECT * FROM VerifKey WHERE keyGame='$key'";
$rowKey = $conn->query($query);

$UserExist = $conn->query("SELECT * FROM users WHERE username='$pseudo' && password='$mdp'");

if($rowKey->num_rows == 1){
	if($UserExist->num_rows == 1){
		
		$row = $UserExist->fetch_array();
		echo "".$row["level"]."|".$row["email"]."|".$row["money"]."|".$row["avatarID"]."";
		
	}else{
		echo "Error login";
	}
}else{
	echo "Error key";
}

?>