-- EJERCICIO 9.6
SET SERVEROUTPUT ON
DEFINE p_apellido = ApellidoInexistente
BEGIN
  DELETE FROM employees
   WHERE last_name = '&p_apellido';

  IF SQL%NOTFOUND THEN
    RAISE_APPLICATION_ERROR(-20089, 'Apellido ingresado invalido');
  END IF;

  ROLLBACK;
END;
/
