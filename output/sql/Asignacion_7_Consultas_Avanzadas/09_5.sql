-- EJERCICIO 9.5
SET SERVEROUTPUT ON
DECLARE
  e_departamento_no_existe EXCEPTION;
  e_cursor_invalido        EXCEPTION;
BEGIN
  DECLARE
    CURSOR c_localidades IS
      SELECT location_id, city
        FROM locations
       WHERE city IN ('Hiroshima', 'Oxford', 'South San Francisco')
       ORDER BY location_id;
    v_localidad locations.location_id%TYPE;
    v_ciudad    locations.city%TYPE;
  BEGIN
    OPEN c_localidades;
    LOOP
      FETCH c_localidades INTO v_localidad, v_ciudad;
      EXIT WHEN c_localidades%NOTFOUND;
      BEGIN
        UPDATE employees
           SET salary = salary * 1.03
         WHERE department_id IN (
               SELECT department_id
                 FROM departments
                WHERE location_id = v_localidad);

        IF SQL%NOTFOUND THEN
          RAISE e_departamento_no_existe;
        END IF;

        DBMS_OUTPUT.PUT_LINE('Localidad ' || v_localidad ||
          ' (' || v_ciudad || '): ' || SQL%ROWCOUNT ||
          ' registros actualizados.');
      EXCEPTION
        WHEN e_departamento_no_existe THEN
          DBMS_OUTPUT.PUT_LINE('Departamento no existe. Favor revise la localidad: ' || v_localidad);
      END;
    END LOOP;
    CLOSE c_localidades;
  EXCEPTION
    WHEN INVALID_CURSOR THEN
      RAISE e_cursor_invalido;
    WHEN OTHERS THEN
      IF c_localidades%ISOPEN THEN CLOSE c_localidades; END IF;
      RAISE;
  END;
  ROLLBACK;
EXCEPTION
  WHEN e_cursor_invalido THEN
    DBMS_OUTPUT.PUT_LINE('Acceso a cursor invalido!');
END;
/
